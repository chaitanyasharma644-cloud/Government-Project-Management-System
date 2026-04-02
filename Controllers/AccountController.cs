using GPMS.Data;
using GPMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace GPMS.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;

        public AccountController(AppDbContext db)
        {
            _db = db;
        }

        public IActionResult Login()
        {
            var captcha = GenerateCaptcha();
            HttpContext.Session.SetString("CaptchaCode", captcha);

            return View(new LoginViewModel
            {
                CaptchaCode = captcha
            });
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var sessionCaptcha = HttpContext.Session.GetString("CaptchaCode");

            if (model.Captcha != sessionCaptcha)
            {
                ModelState.AddModelError("", "Invalid captcha.");
                model.CaptchaCode = GenerateCaptcha();
                HttpContext.Session.SetString("CaptchaCode", model.CaptchaCode);
                return View(model);
            }

            var user = await _db.Employees
                .FirstOrDefaultAsync(x => x.Username == model.Username);

            if (user == null || user.Epassword != model.Password)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                model.CaptchaCode = GenerateCaptcha();
                HttpContext.Session.SetString("CaptchaCode", model.CaptchaCode);
                return View(model);
            }

            // Create Claims for authentication cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.EmployeeName),
                new Claim(ClaimTypes.NameIdentifier, user.EmployeeId.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true
            };

            // Sign in user (creates authentication cookie)
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );

            // Keep session if you still want it
            HttpContext.Session.SetInt32("EmployeeId", user.EmployeeId);
            HttpContext.Session.SetString("EmployeeName", user.EmployeeName);

            return RedirectToAction("Index", "Dashboard");
        }

        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return RedirectToAction("Login");
        }

        private string GenerateCaptcha()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 5)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}