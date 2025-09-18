using Microsoft.EntityFrameworkCore;
using OOOControlSystem.Dtos;
using OOOControlSystem.Models;
using OOOControlSystem.Models.Enums;

namespace OOOControlSystem.Services
{
    public class AuthService
    {
        private readonly ApplicationContext _context;
        private readonly TokenService _tokenService;

        public AuthService(ApplicationContext context, TokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        public async Task<string> Register(RegisterDto registerDto, UserRole role = UserRole.Сustomer)
        {
            if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
                throw new Exception("Пользователь с таким email уже существует");

            var user = new User
            {
                Email = registerDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                FullName = registerDto.FullName,
                Role = role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return _tokenService.CreateToken(user.Id, user.Role, user.TokenVersion);
        }

        public async Task<string> Login(LoginDto loginDto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null)
                throw new Exception("Пользователь не найден");

            if (!user.IsActive)
                throw new Exception("Учетная запись деактивирована");

            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                throw new Exception("Неверный пароль");

            return _tokenService.CreateToken(user.Id, user.Role, user.TokenVersion);
        }
    }
}
