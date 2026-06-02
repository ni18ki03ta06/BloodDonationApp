using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace BloodDonationApp.Helpers
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private const string API_KEY_HEADER = "X-API-Key";

        public ApiKeyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                // Bypass API Key check for authentication endpoints
                if (context.Request.Path.Value != null && context.Request.Path.Value.Contains("/auth/"))
                {
                    await _next(context);
                    return;
                }

                // Bypass API Key check if JWT token is present
                if (context.Request.Headers.TryGetValue("Authorization", out var authHeader) && 
                    authHeader.ToString().StartsWith("Bearer ", System.StringComparison.OrdinalIgnoreCase))
                {
                    await _next(context);
                    return;
                }

                if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedApiKey))
                {
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"error\": \"API Key was not provided.\"}");
                    return;
                }

                var configuredApiKey = configuration["ApiSettings:ApiKey"];

                if (string.IsNullOrEmpty(configuredApiKey) || !configuredApiKey.Equals(extractedApiKey))
                {
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"error\": \"Unauthorized client. Invalid or missing API key.\"}");
                    return;
                }
            }

            await _next(context);
        }
    }
}
