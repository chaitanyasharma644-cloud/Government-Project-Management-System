using GPMS.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GPMS.Filters
{
    public class ForcePasswordChangeFilter : IAsyncActionFilter
    {
        private readonly AppDbContext _db;

        public ForcePasswordChangeFilter(AppDbContext db)
        {
            _db = db;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;

            // ✅ Do nothing if user is not logged in
            if (user?.Identity == null || !user.Identity.IsAuthenticated)
            {
                await next();
                return;
            }

            var controller = context.RouteData.Values["controller"]?.ToString();
            var action = context.RouteData.Values["action"]?.ToString();

            // ✅ Allow these actions
            if (controller == "Account" &&
                (action == "Login" || action == "ChangePassword" || action == "Logout"))
            {
                await next();
                return;
            }

            var claim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null)
            {
                await next();
                return;
            }

            int employeeId = int.Parse(claim.Value);

            var employee = await _db.Employees.FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
            if (employee == null)
            {
                await next();
                return;
            }

            bool passwordExpired = !employee.PasswordChangedAt.HasValue ||
                                   employee.PasswordChangedAt.Value.AddMonths(4) <= DateTime.Now;

            if (employee.IsFirstLogin || passwordExpired)
            {
                context.Result = new RedirectToActionResult("ChangePassword", "Account", null);
                return;
            }

            await next();
        }
    }
}