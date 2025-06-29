using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System;

namespace FriendsCoreAPI.Middleware
{
    public class AppMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AppMiddleware> _logger;

        public AppMiddleware(RequestDelegate next, ILogger<AppMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                string token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

                if (!string.IsNullOrEmpty(token))
                {
                    var handler = new JwtSecurityTokenHandler();

                    if (handler.CanReadToken(token))
                    {
                        var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

                        if (jwtToken != null && jwtToken.ValidTo > DateTime.UtcNow)
                        {
                            var claimsIdentity = new ClaimsIdentity(jwtToken.Claims, "jwt");
                            context.User = new ClaimsPrincipal(claimsIdentity);
                        }
                        else
                        {
                            _logger.LogWarning("JWT is expired or invalid.");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Cannot read JWT token.");
                    }
                }

                await _next(context); // Proceed to next middleware
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in AppMiddleware.");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("An unexpected error occurred.");
            }
        }
    }
}
