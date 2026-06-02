using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BloodDonationApp.Filters
{
    public class AuthFilter : ActionFilterAttribute
    {
        public string? RequiredRole { get; set; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            var userRole = session.GetString("UserRole");
            var adminRole = session.GetString("AdminRole");

            // Check activity timeout if logged in
            if (!string.IsNullOrEmpty(userRole))
            {
                var lastActivityStr = session.GetString("LastActivityAt");
                if (!string.IsNullOrEmpty(lastActivityStr) && DateTime.TryParse(lastActivityStr, out var lastActivity))
                {
                    if (DateTime.UtcNow - lastActivity > TimeSpan.FromMinutes(30))
                    {
                        session.Clear();
                        var controller = context.Controller as Controller;
                        if (controller != null)
                        {
                            controller.TempData["ErrorMessage"] = "Session expired due to inactivity.";
                        }

                        if (RequiredRole == "Admin" || RequiredRole == "SuperAdmin" || userRole == "Admin")
                        {
                            context.Result = new RedirectToActionResult("AdminLogin", "Account", null);
                        }
                        else
                        {
                            context.Result = new RedirectToActionResult("UserLogin", "Account", null);
                        }
                        return;
                    }
                }
                session.SetString("LastActivityAt", DateTime.UtcNow.ToString("o"));
            }

            // 1. SuperAdmin Role Check
            if (RequiredRole == "SuperAdmin")
            {
                if (string.IsNullOrEmpty(userRole) || userRole != "Admin")
                {
                    context.Result = new RedirectToActionResult("AdminLogin", "Account", null);
                    return;
                }
                if (adminRole != "SuperAdmin")
                {
                    var controller = context.Controller as Controller;
                    if (controller != null)
                    {
                        controller.TempData["ErrorMessage"] = "Access Denied: Only SuperAdmin is authorized to perform this action.";
                    }
                    context.Result = new RedirectToActionResult("Index", "Admin", null);
                    return;
                }
            }
            // 2. General Admin Role Check
            else if (RequiredRole == "Admin")
            {
                if (string.IsNullOrEmpty(userRole) || userRole != "Admin" || (adminRole != "Admin" && adminRole != "SuperAdmin"))
                {
                    context.Result = new RedirectToActionResult("AdminLogin", "Account", null);
                    return;
                }
            }
            // 3. General User Role Check
            else if (RequiredRole == "User")
            {
                if (string.IsNullOrEmpty(userRole) || userRole != "User")
                {
                    context.Result = new RedirectToActionResult("UserLogin", "Account", null);
                    return;
                }
            }
            // 4. Fallback for other custom RequiredRole values
            else if (!string.IsNullOrEmpty(RequiredRole) && userRole != RequiredRole)
            {
                if (userRole == "User")
                {
                    context.Result = new RedirectToActionResult("Dashboard", "Donor", null);
                }
                else
                {
                    context.Result = new RedirectToActionResult("Index", "Admin", null);
                }
            }
        }
    }
}
