using System;
using Microsoft.AspNetCore.Http;
using BloodDonationApp.Data;
using BloodDonationApp.Models;

namespace BloodDonationApp.Helpers
{
    public static class AuditService
    {
        public static void LogAction(
            ApplicationDbContext? db,
            HttpContext httpContext,
            string action,
            string entityType,
            int? entityId,
            string details)
        {
            if (db == null && httpContext != null)
            {
                db = (ApplicationDbContext?)httpContext.RequestServices.GetService(typeof(ApplicationDbContext));
            }
            if (db == null) return;

            var session = httpContext?.Session;
            string actorType = "Unknown";
            int actorId = 0;
            string actorName = "Anonymous";

            if (session != null)
            {
                var userRole = session.GetString("UserRole");
                if (userRole == "Admin")
                {
                    actorType = "Admin";
                    var adminIdStr = session.GetString("AdminId");
                    if (int.TryParse(adminIdStr, out var id))
                    {
                        actorId = id;
                    }
                    actorName = session.GetString("AdminName") ?? session.GetString("UserName") ?? "Admin";
                }
                else if (userRole == "User")
                {
                    actorType = "Donor";
                    var userIdStr = session.GetString("UserId");
                    if (int.TryParse(userIdStr, out var id))
                    {
                        actorId = id;
                    }
                    actorName = session.GetString("UserName") ?? "Donor";
                }
            }

            var auditLog = new AuditLog
            {
                ActorType = actorType,
                ActorId = actorId,
                ActorName = actorName,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                Timestamp = DateTime.UtcNow
            };

            db.AuditLogs.Add(auditLog);
            db.SaveChanges();
        }
    }
}
