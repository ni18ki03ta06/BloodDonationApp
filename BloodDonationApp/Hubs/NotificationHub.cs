using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using BloodDonationApp.Data;
using BloodDonationApp.Models;

namespace BloodDonationApp.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public NotificationHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext != null)
            {
                var userIdQuery = httpContext.Request.Query["userId"];
                var roleQuery = httpContext.Request.Query["role"];

                Log.Information("SignalR connection established. ConnectionId: {ConnectionId}, Query - userId: {UserId}, role: {Role}", 
                    Context.ConnectionId, userIdQuery, roleQuery);

                if (!string.IsNullOrEmpty(roleQuery))
                {
                    string role = roleQuery.ToString().Trim();
                    if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) || 
                        string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
                        Log.Information("Added Connection {ConnectionId} to Admins group", Context.ConnectionId);
                    }
                }

                if (!string.IsNullOrEmpty(userIdQuery) && int.TryParse(userIdQuery, out int userId))
                {
                    string donorGroupName = $"Donor_{userId}";
                    await Groups.AddToGroupAsync(Context.ConnectionId, donorGroupName);
                    Log.Information("Added Connection {ConnectionId} to group {GroupName}", Context.ConnectionId, donorGroupName);

                    // Add to BloodType group
                    var donor = await _context.Donors.FindAsync(userId);
                    if (donor != null && !string.IsNullOrEmpty(donor.BloodType))
                    {
                        string bloodGroup = $"BloodType_{donor.BloodType.Trim().ToUpper().Replace(" ", "+")}";
                        await Groups.AddToGroupAsync(Context.ConnectionId, bloodGroup);
                        Log.Information("Added Connection {ConnectionId} to group {GroupName}", Context.ConnectionId, bloodGroup);
                    }
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Log.Information("SignalR connection disconnected. ConnectionId: {ConnectionId}, Exception: {Exception}", 
                Context.ConnectionId, exception?.Message);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
