using CMT.Domain.Data;
using CMT.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;

namespace CMT.Web.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class AmendmentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AmendmentController> _logger;

        public AmendmentController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<AmendmentController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskAttachment>>> GetAttachments()
        {
            return await _context.TaskAttachments.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TaskAttachment>> GetAttachment(int id)
        {
            var attachment = await _context.TaskAttachments.FindAsync(id);

            if (attachment == null)
            {
                return NotFound();
            }

            return attachment;
        }

        [HttpGet("task/{taskId}")]
        public async Task<ActionResult<IEnumerable<TaskAttachment>>> GetAttachmentsByTask(int taskId)
        {
            var attachments = await _context.TaskAttachments
                .Where(a => a.TaskId == taskId)
                .ToListAsync();

            return Ok(attachments);
        }

        [HttpPost]
        public async Task<ActionResult<TaskAttachment>> PostAttachment(TaskAttachment attachment)
        {
            _context.TaskAttachments.Add(attachment);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetAttachment", new { id = attachment.AttachmentId }, attachment);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutAttachment(int id, TaskAttachment attachment)
        {
            if (id != attachment.AttachmentId)
            {
                return BadRequest();
            }

            _context.Entry(attachment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AttachmentExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAttachment(int id)
        {
            var attachment = await _context.TaskAttachments.FindAsync(id);
            if (attachment == null)
            {
                return NotFound();
            }

            _context.TaskAttachments.Remove(attachment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] int taskId)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file uploaded");
                }

                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Create attachment record
                var attachment = new TaskAttachment
                {
                    TaskId = taskId,
                    FileName = file.FileName,
                    FilePath = $"/uploads/{fileName}",
                    FileSize = file.Length,
                    FileType = file.ContentType,
                    UploadedAt = DateTime.UtcNow,
                    UploadedBy = 1 // Default system user ID - you might want to get this from authentication context
                };

                _context.TaskAttachments.Add(attachment);
                await _context.SaveChangesAsync();

                return Ok(attachment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return StatusCode(500, "Error uploading file");
            }
        }

        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadFile(int id)
        {
            try
            {
                var attachment = await _context.TaskAttachments.FindAsync(id);
                if (attachment == null)
                {
                    _logger.LogWarning($"[DOWNLOAD] Attachment {id} not found in database");
                    return NotFound("Attachment not found");
                }

                _logger.LogInformation($"[DOWNLOAD] Attempting to download: {attachment.FileName} (ID: {id})");
                _logger.LogInformation($"[DOWNLOAD] Stored path: {attachment.FilePath}");

                // Enhanced file search logic
                var actualFilePath = FindActualFile(attachment.FilePath, attachment.FileName);

                if (actualFilePath == null)
                {
                    _logger.LogError($"[DOWNLOAD] File not found for attachment {id}: {attachment.FileName}");
                    return NotFound($"Physical file not found: {attachment.FileName}");
                }

                _logger.LogInformation($"[DOWNLOAD] Found file at: {actualFilePath}");

                var fileBytes = await System.IO.File.ReadAllBytesAsync(actualFilePath);
                var contentType = attachment.FileType ?? "application/octet-stream";

                return File(fileBytes, contentType, attachment.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[DOWNLOAD] Error downloading file {id}");
                return StatusCode(500, "Error downloading file");
            }
        }

        private string? FindActualFile(string storedPath, string fileName)
        {
            var searchPaths = new List<string>
            {
                "/workspace/uploads/CMT Eng Task Management/CMT.Web.Api/uploads",
                "/workspace/uploads/CMT Eng Task Management/CMT.Frontend/public/uploads", 
                "/workspace/uploads"
            };

            foreach (var basePath in searchPaths)
            {
                if (!Directory.Exists(basePath)) continue;

                // Try with the stored file path
                if (!string.IsNullOrEmpty(storedPath))
                {
                    var normalizedPath = storedPath.TrimStart('/', '\\');
                    if (normalizedPath.StartsWith("uploads/"))
                    {
                        normalizedPath = normalizedPath.Substring(8);
                    }
                    
                    var fullPath = Path.Combine(basePath, normalizedPath);
                    if (System.IO.File.Exists(fullPath) && new System.IO.FileInfo(fullPath).Length > 0)
                    {
                        _logger.LogInformation($"[FIND-FILE] Found at stored path: {fullPath}");
                        return fullPath;
                    }
                }

                // Try with just the filename
                var fileNamePath = Path.Combine(basePath, Path.GetFileName(fileName));
                if (System.IO.File.Exists(fileNamePath) && new System.IO.FileInfo(fileNamePath).Length > 0)
                {
                    _logger.LogInformation($"[FIND-FILE] Found by filename: {fileNamePath}");
                    return fileNamePath;
                }
            }

            _logger.LogWarning($"[FIND-FILE] File not found in any location: {fileName}");
            return null;
        }

        private bool AttachmentExists(int id)
        {
            return _context.TaskAttachments.Any(e => e.AttachmentId == id);
        }
    }
}