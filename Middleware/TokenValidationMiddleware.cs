using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

namespace OOOControlSystem.Middleware
{
    public class TokenValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public TokenValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ApplicationContext dbContext)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();

                try
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var jwtToken = tokenHandler.ReadJwtToken(token);

                    var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "userId");
                    var versionClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "tokenVersion");

                    if (userIdClaim != null && versionClaim != null &&
                        int.TryParse(userIdClaim.Value, out var userId) &&
                        int.TryParse(versionClaim.Value, out var tokenVersion))
                    {
                        var user = await dbContext.Users
                            .AsNoTracking()
                            .Select(u => new { u.Id, u.TokenVersion, u.IsActive })
                            .FirstOrDefaultAsync(u => u.Id == userId);

                        if (user == null || user.TokenVersion != tokenVersion || !user.IsActive)
                        {
                            context.Response.StatusCode = 401;
                            await context.Response.WriteAsync("Token is invalid or revoked");
                            return;
                        }
                    }
                }
                catch
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Invalid token");
                    return;
                }
            }

            await _next(context);
        }
    }
}
