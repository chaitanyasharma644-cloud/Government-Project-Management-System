using GPMS.Data;
using GPMS.Models;
using GPMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GPMS.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IPasswordHasher<Employee> _passwordHasher;

        public AccountController(AppDbContext db, IPasswordHasher<Employee> passwordHasher)
        {
            _db = db;
            _passwordHasher = passwordHasher;
        }

        // =========================================
        // LOGIN (GET)
        // =========================================
        public IActionResult Login()
        {
            var captcha = GenerateCaptcha();
            HttpContext.Session.SetString("CaptchaCode", captcha);

            return View(new LoginViewModel
            {
                CaptchaCode = captcha
            });
        }

        // =========================================
        // LOGIN (POST)
        // =========================================
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var sessionCaptcha = HttpContext.Session.GetString("CaptchaCode");

            // 🔒 CAPTCHA VALIDATION
            if (model.Captcha != sessionCaptcha)
            {
                ModelState.AddModelError("", "Invalid captcha.");
                return ReloadCaptcha(model);
            }

            // 🔍 USER FETCH
            var user = await _db.Employees
                .FirstOrDefaultAsync(x => x.Username == model.Username);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return ReloadCaptcha(model);
            }

            // 🔐 PASSWORD VALIDATION (HASH + fallback)
            bool passwordValid = false;

            if (!string.IsNullOrWhiteSpace(user.Epassword))
            {
                try
                {
                    var result = _passwordHasher.VerifyHashedPassword(
                        user,
                        user.Epassword,
                        model.Password
                    );

                    passwordValid = result != PasswordVerificationResult.Failed;
                }
                catch (FormatException)
                {
                    // fallback for old plain text passwords
                    passwordValid = user.Epassword == model.Password;
                }
            }

            if (!passwordValid)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return ReloadCaptcha(model);
            }

            // =========================================
            // 🔥 CLAIMS
            // =========================================
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.EmployeeName),
                new Claim(ClaimTypes.NameIdentifier, user.EmployeeId.ToString()),
                new Claim(ClaimTypes.Role, user.SystemRole ?? "User"),
                new Claim("IsAdmin", user.IsAdmin.ToString())
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true }
            );

            // =========================================
            // 🔥 SESSION
            // =========================================
            HttpContext.Session.SetInt32("EmployeeId", user.EmployeeId);
            HttpContext.Session.SetString("EmployeeName", user.EmployeeName);
            HttpContext.Session.SetString("UserRole", user.SystemRole ?? "User");
            HttpContext.Session.SetString("IsAdmin", user.IsAdmin.ToString());

            // =========================================
            // 🔐 PASSWORD POLICY
            // =========================================
            bool passwordExpired =
                !user.PasswordChangedAt.HasValue ||
                user.PasswordChangedAt.Value.AddMonths(4) <= DateTime.Now;

            if (user.IsFirstLogin || passwordExpired)
            {
                HttpContext.Session.SetString("ForcePasswordChange", "true");
                return RedirectToAction("ChangePassword");
            }

            HttpContext.Session.Remove("ForcePasswordChange");

            return RedirectToAction("Index", "Dashboard");
        }

        // =========================================
        // CHANGE PASSWORD (GET)
        // =========================================
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        // =========================================
        // CHANGE PASSWORD (POST)
        // =========================================
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var claim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null)
                return RedirectToAction("Login");

            int employeeId = int.Parse(claim.Value);

            var user = await _db.Employees.FindAsync(employeeId);

            if (user == null)
                return RedirectToAction("Login");

            // 🔐 VERIFY CURRENT PASSWORD
            bool valid = false;

            try
            {
                var result = _passwordHasher.VerifyHashedPassword(
                    user,
                    user.Epassword,
                    model.CurrentPassword
                );

                valid = result != PasswordVerificationResult.Failed;
            }
            catch
            {
                valid = user.Epassword == model.CurrentPassword;
            }

            if (!valid)
            {
                ModelState.AddModelError("CurrentPassword", "Incorrect current password.");
                return View(model);
            }

            if (model.CurrentPassword == model.NewPassword)
            {
                ModelState.AddModelError("NewPassword", "New password must be different.");
                return View(model);
            }

            // 🔐 UPDATE PASSWORD
            user.Epassword = _passwordHasher.HashPassword(user, model.NewPassword);
            user.IsFirstLogin = false;
            user.PasswordChangedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            HttpContext.Session.Remove("ForcePasswordChange");

            TempData["Success"] = "Password changed successfully.";

            return RedirectToAction("Index", "Dashboard");
        }

        // =========================================
        // LOGOUT
        // =========================================
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return RedirectToAction("Login");
        }

        // =========================================
        // 🔁 HELPER: Reload CAPTCHA
        // =========================================
        private IActionResult ReloadCaptcha(LoginViewModel model)
        {
            model.CaptchaCode = GenerateCaptcha();
            HttpContext.Session.SetString("CaptchaCode", model.CaptchaCode);
            return View("Login", model);
        }

        // =========================================
        // 🔢 CAPTCHA GENERATOR
        // =========================================
        private string GenerateCaptcha()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789abcdefghjklmnpqrstuvwxyz";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 5)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}