using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace OOOControlSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : Controller
    {
        private readonly ApplicationContext _context;
        public UsersController(ApplicationContext applicationContext)
        {
            _context = applicationContext;
        }

        // GET: api/users
        [HttpGet]
        //[Authorize(Roles = "Manager")]
        public async Task<ActionResult> GetUsers()
        {
            var users = await _context.Users
            .Where(u => u.IsActive)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FullName,
                u.Role,
                u.IsActive,
                u.CreatedAt
            })
            .ToListAsync();

            return Ok(users);
        }
    }
}