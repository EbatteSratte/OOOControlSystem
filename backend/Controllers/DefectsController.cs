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
    public class DefectsController : Controller
    {
        private readonly ApplicationContext _context;
        private readonly IWebHostEnvironment _env;
        public DefectsController(ApplicationContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: api/defects
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetDefects(
        [FromQuery] int? projectId,
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] int? assignedToId,
        [FromQuery] int? reportedById,
        [FromQuery] string? search,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] string? sortDir = "desc")
        {
            var query = _context.Defects
            .AsNoTracking()
            .Include(d => d.Project)
            .Include(d => d.Reporter)
            .Include(d => d.AssignedUser)
            .AsQueryable();


            if (projectId.HasValue)
                query = query.Where(d => d.ProjectId == projectId.Value);


            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<DefectStatus>(status, true, out var st))
                query = query.Where(d => d.Status == st);


            if (!string.IsNullOrWhiteSpace(priority) && Enum.TryParse<DefectPriority>(priority, true, out var pr))
                query = query.Where(d => d.Priority == pr);


            if (assignedToId.HasValue)
                query = query.Where(d => d.AssignedToId == assignedToId.Value);


            if (reportedById.HasValue)
                query = query.Where(d => d.ReportedById == reportedById.Value);


            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(d => d.Title.Contains(search) || (d.Description != null && d.Description.Contains(search)));
            }


            bool desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
            query = sortBy?.ToLower() switch
            {
                "priority" => (desc ? query.OrderByDescending(d => d.Priority) : query.OrderBy(d => d.Priority)),
                "status" => (desc ? query.OrderByDescending(d => d.Status) : query.OrderBy(d => d.Status)),
                _ => (desc ? query.OrderByDescending(d => d.CreatedAt) : query.OrderBy(d => d.CreatedAt))
            };

            var defects = await query.ToListAsync();

            var items = defects.Select(MapDefectToListItem).ToList(); ;


            return Ok(items);
        }

        // GET: api/defects/{id}
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDefect(int id)
        {
            var defect = await _context.Defects
            .Include(d => d.Project)
            .Include(d => d.Reporter)
            .Include(d => d.AssignedUser)
            .FirstOrDefaultAsync(d => d.Id == id);


            if (defect == null)
                return NotFound(new { Message = "Дефект не найден" });


            return Ok(MapDefectToDetails(defect));
        }

        // POST: api/defects
        [Authorize(Roles = "Engineer")]
        [HttpPost]
        public async Task<IActionResult> CreateDefect([FromBody] DefectCreateDto dto)
        {
            var userId = int.Parse(User.FindFirst("userId")!.Value);


            var project = await _context.Projects.FindAsync(dto.ProjectId);
            if (project == null)
                return BadRequest(new { Message = "Проект не найден" });


            User? assignee = null;
            if (dto.AssignedToId.HasValue)
            {
                assignee = await _context.Users.FindAsync(dto.AssignedToId.Value);
                if (assignee == null)
                    return BadRequest(new { Message = "Назначенный пользователь не найден" });
            }


            if (!Enum.TryParse<DefectPriority>(dto.Priority, true, out var priority))
                return BadRequest(new { Message = "Недопустимый приоритет" });


            var defect = new Defect
            {
                Title = dto.Title,
                Description = dto.Description,
                Priority = priority,
                Status = DefectStatus.New,
                ProjectId = dto.ProjectId,
                AssignedToId = dto.AssignedToId,
                ReportedById = userId,
                DueDate = dto.DueDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                History = new List<Defect.DefectHistoryEntry>{
new Defect.DefectHistoryEntry{ Status = DefectStatus.New, ChangedAt = DateTime.UtcNow, ChangedById = userId, Description = dto.Description ?? "" }
}
            };


            _context.Defects.Add(defect);
            await _context.SaveChangesAsync();


            return CreatedAtAction(nameof(GetDefect), new { id = defect.Id }, MapDefectToDetails(defect));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Engineer")]
        public async Task<IActionResult> UpdateDefect(int id, [FromBody] DefectUpdateDto dto)
        {
            var currentUserId = int.Parse(User.FindFirst("userId")!.Value);
            var currentUserRole = User.FindFirst("role")?.Value;


            var defect = await _context.Defects.Include(d => d.Reporter).FirstOrDefaultAsync(d => d.Id == id);
            if (defect == null)
                return NotFound(new { Message = "Дефект не найден" });


            var isManager = string.Equals(currentUserRole, nameof(UserRole.Manager), StringComparison.OrdinalIgnoreCase);
            var isReporter = defect.ReportedById == currentUserId;
            var isAssignee = defect.AssignedToId == currentUserId;
            if (!(isManager || isReporter || isAssignee))
                return Forbid();


            if (!string.IsNullOrWhiteSpace(dto.Title)) defect.Title = dto.Title;
            if (dto.Description != null) defect.Description = dto.Description;
            if (dto.DueDate.HasValue) defect.DueDate = dto.DueDate;
            if (!string.IsNullOrWhiteSpace(dto.Priority))
            {
                if (!Enum.TryParse<DefectPriority>(dto.Priority, true, out var pr))
                    return BadRequest(new { Message = "Недопустимый приоритет" });
                defect.Priority = pr;
            }


            defect.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Дефект успешно обновлён" });
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Engineer")]
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] DefectStatusDto dto)
        {
            var currentUserId = int.Parse(User.FindFirst("userId")!.Value);


            var defect = await _context.Defects.FindAsync(id);
            if (defect == null)
                return NotFound(new { Message = "Дефект не найден" });


            if (!Enum.TryParse<DefectStatus>(dto.Status, true, out var newStatus))
                return BadRequest(new { Message = "Недопустимый статус" });


            if (defect.Status == newStatus)
                return Ok(new { Message = "Статус не изменился" });


            defect.Status = newStatus;
            defect.UpdatedAt = DateTime.UtcNow;
            AppendHistory(defect, newStatus, currentUserId, dto.StatusDescription);


            await _context.SaveChangesAsync();
            return Ok(new { Message = "Статус обновлён" });
        }

        [HttpPut("{id}/assign")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Assign(int id, [FromBody] DefectAssignDto dto)
        {
            var currentUserId = int.Parse(User.FindFirst("userId")!.Value);
            var currentUserName = User.FindFirst("fullName")?.Value ?? string.Empty;


            var defect = await _context.Defects.FindAsync(id);
            if (defect == null)
                return NotFound(new { Message = "Дефект не найден" });


            if (dto.AssignedToId.HasValue)
            {
                var assignee = await _context.Users.FindAsync(dto.AssignedToId.Value);
                if (assignee == null)
                    return BadRequest(new { Message = "Назначенный пользователь не найден" });
            }


            defect.AssignedToId = dto.AssignedToId;
            defect.UpdatedAt = DateTime.UtcNow;


            await _context.SaveChangesAsync();
            return Ok(new { Message = "Исполнитель обновлён" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Engineer")]
        public async Task<IActionResult> DeleteDefect(int id)
        {
            var defect = await _context.Defects.FindAsync(id);
            if (defect == null)
                return NotFound(new { Message = "Дефект не найден" });

            _context.Defects.Remove(defect);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Дефект удалён" });
        }
        [HttpPost("{id}/attachments")]
        [Authorize(Roles = "Engineer")]
        public async Task<IActionResult> UploadAttachments(int id, [FromForm] List<IFormFile> files)
        {
            var defect = await _context.Defects.FindAsync(id);
            if (defect == null) return NotFound();

            if (defect.AttachmentPaths == null)
                defect.AttachmentPaths = new List<string>();

            var uploadRoot = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "defects", id.ToString());
            if (!Directory.Exists(uploadRoot))
                Directory.CreateDirectory(uploadRoot);

            var savedPaths = new List<string>();

            foreach (var file in files)
            {
                if (file.Length <= 0) continue;

                var ext = Path.GetExtension(file.FileName);
                var safeName = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Path.GetFileNameWithoutExtension(file.FileName)}{ext}";
                var fullPath = Path.Combine(uploadRoot, safeName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var relativePath = $"/uploads/defects/{id}/{safeName}";
                defect.AttachmentPaths.Add(relativePath);
                savedPaths.Add(relativePath);
            }

            _context.Defects.Update(defect);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Файлы загружены", paths = savedPaths });
        }

        [HttpGet("{id}/attachments")]
        [Authorize]
        public async Task<IActionResult> GetAttachments(int id)
        {
            var defect = await _context.Defects
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);

            if (defect == null) return NotFound();

            return Ok(defect.AttachmentPaths ?? new List<string>());
        }

        [HttpGet("{id}/attachments/{fileName}")]
        [Authorize]
        public IActionResult GetAttachmentFile(int id, string fileName)
        {
            var folder = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "defects", id.ToString());
            var fullPath = Path.Combine(folder, fileName);

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var mime = "application/octet-stream";
            return PhysicalFile(fullPath, mime, enableRangeProcessing: true);
        }

        private static object MapDefectToListItem(Defect d) => new
        {
            d.Id,
            d.Title,
            d.Description,
            Status = d.Status.ToString(),
            Priority = d.Priority.ToString(),
            d.ProjectId,
            Project = d.Project != null ? new { d.Project.Id, d.Project.Name } : null,
            Reporter = d.Reporter != null ? new { d.Reporter.Id, d.Reporter.FullName } : null,
            AssignedTo = d.AssignedUser != null ? new { d.AssignedUser.Id, d.AssignedUser.FullName } : null,
            d.DueDate,
            d.CreatedAt,
            d.UpdatedAt
        };


        private static object MapDefectToDetails(Defect d) => new
        {
            d.Id,
            d.Title,
            d.Description,
            Status = d.Status.ToString(),
            Priority = d.Priority.ToString(),
            Project = d.Project != null ? new { d.Project.Id, d.Project.Name } : null,
            Reporter = d.Reporter != null ? new { d.Reporter.Id, d.Reporter.FullName, d.Reporter.Email } : null,
            AssignedTo = d.AssignedUser != null ? new { d.AssignedUser.Id, d.AssignedUser.FullName, d.AssignedUser.Email } : null,
            d.DueDate,
            d.AttachmentPaths,
            d.History,
            d.CreatedAt,
            d.UpdatedAt
        };

        private static void AppendHistory(Defect defect, DefectStatus status, int byId, string description)
        {
            var list = defect.History?.ToList() ?? new List<Defect.DefectHistoryEntry>();
            list.Add(new Defect.DefectHistoryEntry
            {
                Status = status,
                ChangedAt = DateTime.UtcNow,
                ChangedById = byId,
                Description = description
            });
            defect.History = list;
        }
    }
}
