using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GPMS.Filters
{
    public class ForcePasswordChangeFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            var forcePasswordChange = session.GetString("ForcePasswordChange");

            var controller = context.RouteData.Values["controller"]?.ToString();
            var action = context.RouteData.Values["action"]?.ToString();

            // Allow these actions without redirection
            bool isAllowedAction =
                controller == "Account" &&
                (
                    action == "Login" ||
                    action == "Logout" ||
                    action == "ChangePassword" ||
                    action == "ForgotPassword" ||
                    action == "ResetPassword"
                );

            if (forcePasswordChange == "true" && !isAllowedAction)
            {
                context.Result = new RedirectToActionResult(
                    "ChangePassword",
                    "Account",
                    null
                );
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}