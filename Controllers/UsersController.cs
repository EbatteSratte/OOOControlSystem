using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OOOControlSystem.Dtos;
using OOOControlSystem.Models;
using OOOControlSystem.Models.Enums;
using OOOControlSystem.Services;

namespace OOOControlSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : Controller
    {
        private readonly ApplicationContext _context;
        private readonly TokenService _tokenService;
        public UsersController(ApplicationContext applicationContext, TokenService tokenService)
        {
            _context = applicationContext;
            _tokenService = tokenService;
        }

        // GET: api/users/get-profile
        [HttpGet("get-profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var userId = int.Parse(User.FindFirst("userId")?.Value!);

            var user = await _context.Users
                .Include(u => u.CreatedProjects)
                .Include(u => u.ReportedDefects)
                .Include(u => u.AssignedDefects)
                .Include(u => u.OwnedProjects)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound();

            return Ok(MapUserToResponse(user, true));
        }

        // GET: api/users?isActive=true
        [HttpGet]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult> GetUsers(int? id = null, bool? isActive = true)
        {
            if (id.HasValue)
            {
                var user = await _context.Users
                    .Where(u => u.Id == id.Value)
                    .Include(u => u.CreatedProjects)
                    .Include(u => u.ReportedDefects)
                    .Include(u => u.AssignedDefects)
                    .Include(u => u.OwnedProjects)
                    .FirstOrDefaultAsync();

                if (user == null)
                    return NotFound(new { Message = "Пользователь не найден" });

                return Ok(MapUserToResponse(user, true));
            }

            var query = _context.Users.AsQueryable();

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            var users = await query
                .Include(u => u.CreatedProjects)
                .Include(u => u.ReportedDefects)
                .Include(u => u.AssignedDefects)
                .Include(u => u.OwnedProjects)
                .ToListAsync();

            var response = users.Select(u => MapUserToResponse(u, false)).ToList();
            return Ok(response);
        }

        // PUT: api/users/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateUser(int id, UserUpdateDto updateDto)
        {
            var currentUserId = int.Parse(User.FindFirst("userId")?.Value!);

            var currentUserFromDb = await _context.Users.FindAsync(currentUserId);
            if (currentUserFromDb == null)
                return Unauthorized();

            var userToUpdate = await _context.Users.FindAsync(id);
            if (userToUpdate == null)
                return NotFound(new { Message = "Пользователь не найден" });

            if (currentUserFromDb.Role != UserRole.Manager && currentUserId != id)
                return Forbid();

            if (!string.IsNullOrEmpty(updateDto.FullName))
                userToUpdate.FullName = updateDto.FullName;

            if (!string.IsNullOrEmpty(updateDto.Email))
            {
                if (await _context.Users.AnyAsync(u => u.Email == updateDto.Email && u.Id != id))
                    return BadRequest(new { Message = "Email уже занят" });

                userToUpdate.Email = updateDto.Email;
            }

            if (updateDto.IsActive.HasValue && currentUserFromDb.Role == UserRole.Manager)
                userToUpdate.IsActive = updateDto.IsActive.Value;

            if (!string.IsNullOrEmpty(updateDto.Role) && currentUserFromDb.Role == UserRole.Manager)
            {
                if (Enum.TryParse<UserRole>(updateDto.Role, out var newRole))
                    userToUpdate.Role = newRole;
                else
                    return BadRequest(new { Message = "Недопустимая роль" });
                await _tokenService.InvalidateTokens(id);
            }

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Пользователь успешно обновлен" });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { Message = "Ошибка при обновлении пользователя", Error = ex.Message });
            }
        }

        private object MapUserToResponse(User user, bool full = false)
        {
            var baseResponse = new
            {
                user.Id,
                user.Email,
                user.FullName,
                Role = user.Role.ToString(),
                user.IsActive,
                user.CreatedAt
            };

            if (!full)
                return baseResponse;

            return new
            {
                user.Id,
                user.Email,
                user.FullName,
                Role = user.Role.ToString(),
                user.IsActive,
                user.CreatedAt,
                CreatedProjects = user.CreatedProjects.Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Status,
                    p.CreatedAt
                }).ToList(),
                OwnedProjects = user.OwnedProjects.Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Status,
                    p.CreatedAt
                }).ToList(),
                ReportedDefects = user.ReportedDefects.Select(d => new
                {
                    d.Id,
                    d.Title,
                    d.Status,
                    d.Priority,
                    d.CreatedAt
                }).ToList(),
                AssignedDefects = user.AssignedDefects.Select(d => new
                {
                    d.Id,
                    d.Title,
                    d.Status,
                    d.Priority,
                    d.CreatedAt
                }).ToList()
            };
        }
    }
}