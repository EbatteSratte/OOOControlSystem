using Microsoft.IdentityModel.Tokens;
using OOOControlSystem.Models;
using OOOControlSystem.Models.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OOOControlSystem.Services
{
    public class TokenService
    {
        private readonly IConfiguration _config;
        private readonly ApplicationContext _context;

        public TokenService(IConfiguration config, ApplicationContext context)
        {
            _config = config;
            _context = context;
        }

        public string CreateToken(int id, UserRole role, int tokenVersion)
        {
            var claims = new List<Claim>
            {
                new Claim("userId", id.ToString()),
                new Claim(ClaimTypes.Role, role.ToString()),
                new Claim("tokenVersion", tokenVersion.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = creds,
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
        public async Task InvalidateTokens(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.TokenVersion++;
                await _context.SaveChangesAsync();
            }
        }
    }
}
