using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace OOOControlSystem.Controllers
{
    [ApiController]
    [Route("api/uploads")]
    public class UploadsController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        public UploadsController(IWebHostEnvironment env) => _env = env;

        [HttpGet("defects/{id:int}/{fileName}")]
        [Authorize]
        public IActionResult GetDefectFile(int id, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) ||
                fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return BadRequest("Invalid file name.");

            fileName = Path.GetFileName(fileName);

            var webroot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var folder = Path.Combine(webroot, "uploads", "defects", id.ToString());
            var full = Path.Combine(folder, fileName);

            if (!System.IO.File.Exists(full))
                return NotFound();

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(full, out var contentType))
                contentType = "application/octet-stream";

            Response.Headers["Cache-Control"] = "public, max-age=86400";
            Response.Headers["Accept-Ranges"] = "bytes";

            return PhysicalFile(full, contentType, enableRangeProcessing: true);
        }
    }
}
