using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OOOControlSystem.Dtos;
using OOOControlSystem.Models;
using OOOControlSystem.Models.Enums;

namespace OOOControlSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : Controller
    {
        private readonly ApplicationContext _context;
        public ProjectsController(ApplicationContext applicationContext)
        {
            _context = applicationContext;
        }

        // GET: api/projects
        [Authorize]
        [HttpGet]
        public async Task<ActionResult> GetProjects()
        {
            var projects = await _context.Projects
                .Include(p => p.Creator)
                .Include(p => p.Defects)
                .Include(p => p.Owner)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    Status = p.Status.ToString(),
                    p.CreatedAt,
                    Creator = new { p.Creator.Id, p.Creator.FullName },
                    Owner = new { p.Owner.Id, p.Owner.FullName },
                })
                .ToListAsync();

            return Ok(projects);
        }

        // GET: api/projects/id
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult> GetProject(int id)
        {
            var project = await _context.Projects
                .Where(p => p.Id == id)
                .Include(p => p.Creator)
                .Include(p => p.Owner)
                .Include(p => p.Defects)
                    .ThenInclude(d => d.Reporter)
                .Include(p => p.Defects)
                    .ThenInclude(d => d.AssignedUser)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    Status = p.Status.ToString(),
                    p.CreatedAt,
                    Creator = new { p.Creator.Id, p.Creator.FullName, p.Creator.Email },
                    Owner = new { p.Owner.Id, p.Owner.FullName },
                    Defects = p.Defects.Select(d => new
                    {
                        d.Id,
                        d.Title,
                        d.Description,
                        Status = d.Status.ToString(),
                        Priority = d.Priority.ToString(),
                        d.CreatedAt,
                        Reporter = new { d.Reporter.Id, d.Reporter.FullName },
                        AssignedTo = d.AssignedUser != null ? new { d.AssignedUser.Id, d.AssignedUser.FullName } : null
                    })
                })
                .FirstOrDefaultAsync();

            if (project == null)
                return NotFound(new { Message = "Проект не найден" });

            return Ok(project);
        }

        // POST: api/projects
        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> CreateProject(ProjectCreateDto createDto)
        {
            var userId = int.Parse(User.FindFirst("userId")?.Value!);

            var owner = await _context.Users.FindAsync(createDto.OwnerId);
            if (owner == null)
                return BadRequest(new { Message = "Владелец не найден" });

            var project = new Project
            {
                Name = createDto.Name,
                Description = createDto.Description,
                Status = ProjectStatus.Active,
                CreatedById = userId,
                OwnerId = createDto.OwnerId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project);
        }

        // PUT: api/projects/id
        [HttpPut("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UpdateProject(int id, ProjectUpdateDto updateDto)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
                return NotFound(new { Message = "Проект не найден" });

            if (!string.IsNullOrEmpty(updateDto.Name))
                project.Name = updateDto.Name;

            if (!string.IsNullOrEmpty(updateDto.Description))
                project.Description = updateDto.Description;

            if (!string.IsNullOrEmpty(updateDto.Status))
            {
                if (Enum.TryParse<ProjectStatus>(updateDto.Status, out var newStatus))
                    project.Status = newStatus;
                else
                    return BadRequest(new { Message = "Недопустимый статус" });
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Проект успешно обновлен" });
        }

        // DELETE: api/projects/id
        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
                return NotFound(new { Message = "Проект не найден" });

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Проект успешно удален" });
        }
    }
}
