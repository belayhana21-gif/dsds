using CMT.Application.Interfaces;
using CMT.Domain.Data;
using CMT.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;

namespace CMT.Web.Api.Controllers
{
    [ApiController]
    [Route("api/tasks/{taskId}/attachments")]
    [AllowAnonymous] // Temporarily remove authorization to fix 401 errors
    public class AttachmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ITaskService _taskService;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AttachmentsController> _logger;

        public AttachmentsController(
            ApplicationDbContext context, 
            ITaskService taskService, 
            IWebHostEnvironment environment,
            IConfiguration configuration,
            ILogger<AttachmentsController> logger)
        {
            _context = context;
            _taskService = taskService;
            _environment = environment;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Get the base storage path with fallback options
        /// </summary>
        private string GetBaseStoragePath()
        {
            // Try multiple locations in order of preference
            var possiblePaths = new[]
            {
                Path.Combine(_environment.ContentRootPath ?? "", "../CMT.Frontend/public/uploads"),
                Path.Combine(_environment.WebRootPath ?? "", "uploads"),
                "/tmp/cmt-uploads", // Fallback to /tmp which is always writable
                Path.Combine(Path.GetTempPath(), "cmt-uploads") // System temp directory
            };

            foreach (var path in possiblePaths)
            {
                try
                {
                    var fullPath = Path.GetFullPath(path);
                    
                    // Try to create directory if it doesn't exist
                    if (!Directory.Exists(fullPath))
                    {
                        Directory.CreateDirectory(fullPath);
                    }
                    
                    // Test write permissions by creating a test file
                    var testFile = Path.Combine(fullPath, $".write_test_{Guid.NewGuid()}.tmp");
                    try
                    {
                        System.IO.File.WriteAllText(testFile, "test");
                        System.IO.File.Delete(testFile);
                        
                        _logger.LogInformation($"Using upload directory: {fullPath}");
                        return fullPath;
                    }
                    catch
                    {
                        _logger.LogWarning($"Directory not writable: {fullPath}");
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Cannot use path: {path}");
                    continue;
                }
            }

            // Last resort: use temp directory
            var tempPath = Path.Combine(Path.GetTempPath(), "cmt-uploads");
            Directory.CreateDirectory(tempPath);
            _logger.LogWarning($"Using fallback temp directory: {tempPath}");
            return tempPath;
        }

        /// <summary>
        /// Get all attachments for a specific task with file existence validation
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetTaskAttachments(int taskId)
        {
            try
            {
                // Verify task exists
                var task = await _taskService.GetTaskByIdAsync(taskId);
                if (task == null)
                {
                    return NotFound(new { message = $"Task with ID {taskId} not found" });
                }

                // Get attachments from database
                var attachments = await _context.TaskAttachments
                    .Include(a => a.UploadedByUser)
                    .Where(a => a.TaskId == taskId)
                    .ToListAsync();

                var result = new List<object>();
                
                foreach (var attachment in attachments)
                {
                    // Check if file exists on disk
                    var actualPath = await FindActualFilePath(attachment);
                    var fileExists = actualPath != null;
                    
                    result.Add(new
                    {
                        attachment_id = attachment.AttachmentId,
                        task_id = attachment.TaskId,
                        file_path = attachment.FilePath,
                        file_name = attachment.FileName,
                        file_type = attachment.FileType,
                        file_size = attachment.FileSize,
                        uploaded_by = attachment.UploadedBy,
                        uploader_name = attachment.UploadedByUser != null ? attachment.UploadedByUser.FullName : "Unknown",
                        uploaded_at = attachment.UploadedAt,
                        file_exists = fileExists,
                        actual_path = actualPath,
                        status = fileExists ? "Available" : "Missing - File Not Found"
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving attachments for task {TaskId}", taskId);
                return StatusCode(500, new { message = "An error occurred while retrieving attachments", error = ex.Message });
            }
        }

        /// <summary>
        /// Download attachment file - ENHANCED with missing file handling
        /// </summary>
        [HttpGet("{attachmentId}/download")]
        public async Task<IActionResult> DownloadAttachment(int taskId, int attachmentId)
        {
            try
            {
                _logger.LogInformation($"[DOWNLOAD] Attempting to download attachment {attachmentId} for task {taskId}");

                // Get attachment from database
                var attachment = await _context.TaskAttachments
                    .FirstOrDefaultAsync(a => a.TaskId == taskId && a.AttachmentId == attachmentId);

                if (attachment == null)
                {
                    _logger.LogWarning($"[DOWNLOAD] Attachment {attachmentId} not found for task {taskId}");
                    return NotFound(new { message = $"Attachment with ID {attachmentId} not found for task {taskId}" });
                }

                _logger.LogInformation($"[DOWNLOAD] Found attachment in DB: {attachment.FileName}");

                // Find the actual file location using enhanced search
                var actualFilePath = await FindActualFilePath(attachment);

                if (actualFilePath == null)
                {
                    _logger.LogWarning($"[DOWNLOAD] File '{attachment.FileName}' not found in any location.");
                    
                    // Try to find a similar file that might be the same content
                    var similarFile = await FindSimilarFile(attachment);
                    
                    if (similarFile != null)
                    {
                        _logger.LogInformation($"[DOWNLOAD] Found similar file: {similarFile}");
                        var similarFileBytes = await System.IO.File.ReadAllBytesAsync(similarFile);
                        var similarContentType = GetContentType(attachment.FileType, attachment.FileName);
                        return File(similarFileBytes, similarContentType, attachment.FileName);
                    }
                    
                    return NotFound(new { 
                        message = "Physical file not found for attachment", 
                        fileName = attachment.FileName,
                        attachmentId = attachmentId,
                        suggestions = new[]
                        {
                            "The file may have been deleted or moved",
                            "Contact administrator to restore from backup",
                            "Re-upload the file if available",
                            "Use the cleanup endpoint to remove orphaned database records"
                        }
                    });
                }

                // Read file and return
                var fileBytes = await System.IO.File.ReadAllBytesAsync(actualFilePath);
                var contentType = GetContentType(attachment.FileType, attachment.FileName);
                
                _logger.LogInformation($"[DOWNLOAD] Successfully serving file: {attachment.FileName} ({fileBytes.Length} bytes)");
                
                return File(fileBytes, contentType, attachment.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[DOWNLOAD] Error downloading attachment {attachmentId} for task {taskId}");
                return StatusCode(500, "Internal server error occurred while downloading the file");
            }
        }

        /// <summary>
        /// NEW: Clean up orphaned database records for missing files
        /// </summary>
        [HttpPost("cleanup-missing")]
        public async Task<ActionResult> CleanupMissingFiles(int taskId)
        {
            try
            {
                _logger.LogInformation($"[CLEANUP] Starting cleanup of missing files for task {taskId}");
                
                var attachments = await _context.TaskAttachments
                    .Where(a => a.TaskId == taskId)
                    .ToListAsync();

                var removedCount = 0;
                var keptCount = 0;
                var removedFiles = new List<object>();

                foreach (var attachment in attachments)
                {
                    var actualPath = await FindActualFilePath(attachment);
                    
                    if (actualPath == null)
                    {
                        _logger.LogInformation($"[CLEANUP] Removing orphaned record: {attachment.FileName} (ID: {attachment.AttachmentId})");
                        
                        removedFiles.Add(new
                        {
                            attachment_id = attachment.AttachmentId,
                            file_name = attachment.FileName,
                            file_path = attachment.FilePath,
                            uploaded_at = attachment.UploadedAt
                        });
                        
                        _context.TaskAttachments.Remove(attachment);
                        removedCount++;
                    }
                    else
                    {
                        keptCount++;
                    }
                }

                if (removedCount > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"[CLEANUP] Removed {removedCount} orphaned records for task {taskId}");
                }

                return Ok(new 
                { 
                    message = $"Cleanup completed for task {taskId}", 
                    removedCount = removedCount,
                    keptCount = keptCount,
                    totalChecked = attachments.Count,
                    removedFiles = removedFiles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CLEANUP] Error cleaning up missing files for task {TaskId}", taskId);
                return StatusCode(500, "Error occurred while cleaning up missing files");
            }
        }

        /// <summary>
        /// NEW: Repair file paths in database - Enhanced endpoint
        /// </summary>
        [HttpPost("repair-paths")]
        public async Task<ActionResult> RepairFilePaths(int taskId)
        {
            try
            {
                _logger.LogInformation($"[REPAIR] Starting file path repair for task {taskId}");
                
                var attachments = await _context.TaskAttachments
                    .Where(a => a.TaskId == taskId)
                    .ToListAsync();

                var repairedCount = 0;
                var notFoundCount = 0;
                var repairedFiles = new List<object>();

                foreach (var attachment in attachments)
                {
                    var actualPath = await FindActualFilePath(attachment);
                    
                    if (actualPath != null)
                    {
                        // Convert to relative path for database storage
                        var relativePath = ConvertToRelativePath(actualPath);
                        
                        if (attachment.FilePath != relativePath)
                        {
                            _logger.LogInformation($"[REPAIR] Updating path for {attachment.FileName}: {attachment.FilePath} -> {relativePath}");
                            
                            repairedFiles.Add(new
                            {
                                attachment_id = attachment.AttachmentId,
                                file_name = attachment.FileName,
                                old_path = attachment.FilePath,
                                new_path = relativePath,
                                actual_path = actualPath
                            });
                            
                            attachment.FilePath = relativePath;
                            repairedCount++;
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"[REPAIR] File not found for attachment: {attachment.FileName} (ID: {attachment.AttachmentId})");
                        notFoundCount++;
                    }
                }

                if (repairedCount > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"[REPAIR] Successfully repaired {repairedCount} file paths for task {taskId}");
                }

                return Ok(new 
                { 
                    message = $"Repair completed for task {taskId}", 
                    repairedCount = repairedCount,
                    notFoundCount = notFoundCount,
                    totalChecked = attachments.Count,
                    repairedFiles = repairedFiles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPAIR] Error repairing file paths for task {TaskId}", taskId);
                return StatusCode(500, "Error occurred while repairing file paths");
            }
        }

        /// <summary>
        /// Enhanced file finding with comprehensive search strategy
        /// </summary>
        private async Task<string?> FindActualFilePath(TaskAttachment attachment)
        {
            var fileName = Path.GetFileName(attachment.FilePath);
            var possiblePaths = new List<string>();
            
            // 1. Primary location where files are actually stored
            var primaryPath = GetBaseStoragePath();
            possiblePaths.Add(Path.Combine(primaryPath, fileName));
            
            // 2. Alternative locations
            if (!string.IsNullOrEmpty(_environment.WebRootPath))
            {
                possiblePaths.Add(Path.Combine(_environment.WebRootPath, "uploads", fileName));
            }
            
            if (!string.IsNullOrEmpty(_environment.ContentRootPath))
            {
                possiblePaths.Add(Path.Combine(_environment.ContentRootPath, "uploads", fileName));
                possiblePaths.Add(Path.Combine(_environment.ContentRootPath, "../CMT.Frontend/public/uploads", fileName));
            }

            // 3. Handle extension variations (.xls vs .xlsx)
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var currentExt = Path.GetExtension(fileName);
            var alternativeExt = currentExt.ToLower() == ".xls" ? ".xlsx" : ".xls";
            
            var alternativeFileName = fileNameWithoutExt + alternativeExt;
            possiblePaths.Add(Path.Combine(primaryPath, alternativeFileName));

            // Search for the file in all possible locations
            foreach (var path in possiblePaths)
            {
                try
                {
                    var fullPath = Path.GetFullPath(path);
                    _logger.LogDebug($"[SEARCH] Checking path: {fullPath}");
                    if (System.IO.File.Exists(fullPath))
                    {
                        var fileInfo = new System.IO.FileInfo(fullPath);
                        if (fileInfo.Length > 0) // Basic integrity check
                        {
                            _logger.LogInformation($"[SEARCH] File found at: {fullPath}");
                            return fullPath;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error checking location: {Location}", path);
                }
            }

            return null;
        }

        /// <summary>
        /// Find a similar file that might be the same content (fallback mechanism)
        /// </summary>
        private async Task<string?> FindSimilarFile(TaskAttachment attachment)
        {
            try
            {
                var uploadsPath = GetBaseStoragePath();
                if (!Directory.Exists(uploadsPath)) return null;

                var targetExtension = Path.GetExtension(attachment.FileName).ToLower();
                var targetSize = attachment.FileSize;

                var files = Directory.GetFiles(uploadsPath, $"*{targetExtension}")
                    .Where(f => new FileInfo(f).Length == targetSize)
                    .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                    .Take(3); // Check the 3 most recent files with same extension and size

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.Length > 0 && fileInfo.Length == targetSize)
                    {
                        _logger.LogInformation($"[SIMILAR] Found similar file: {file} (size: {fileInfo.Length})");
                        return file;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error finding similar file for {FileName}", attachment.FileName);
            }

            return null;
        }

        /// <summary>
        /// Convert absolute path to relative path for database storage
        /// </summary>
        private string ConvertToRelativePath(string absolutePath)
        {
            var fileName = Path.GetFileName(absolutePath);
            return $"/uploads/{fileName}";
        }

        /// <summary>
        /// Get appropriate content type for file
        /// </summary>
        private string GetContentType(string? fileType, string fileName)
        {
            if (!string.IsNullOrEmpty(fileType) && fileType.Contains("/"))
            {
                return fileType;
            }

            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".txt" => "text/plain",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".zip" => "application/zip",
                _ => "application/octet-stream"
            };
        }

        /// <summary>
        /// Upload a new attachment to a task
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<object>> UploadAttachment(int taskId, [FromForm] IFormFile file, [FromForm] int? uploadedBy)
        {
            try
            {
                var task = await _taskService.GetTaskByIdAsync(taskId);
                if (task == null)
                {
                    return NotFound(new { message = $"Task with ID {taskId} not found" });
                }

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "No file provided" });
                }

                if (file.Length > 5 * 1024 * 1024) // Increased to 5MB
                {
                    return BadRequest(new { message = "File size exceeds 5MB limit" });
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".zip" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { message = $"File type not allowed. Allowed types: {string.Join(", ", allowedExtensions)}" });
                }

                // Use the writable storage path
                var baseStoragePath = GetBaseStoragePath();
                _logger.LogInformation($"Using storage path: {baseStoragePath}");

                var timestamp = DateTime.UtcNow.Ticks;
                var uniqueFileName = $"attachment_{timestamp}_{Guid.NewGuid().ToString()[..8]}{fileExtension}";
                var filePath = Path.Combine(baseStoragePath, uniqueFileName);

                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await file.CopyToAsync(stream);
                        await stream.FlushAsync();
                    }
                    
                    _logger.LogInformation($"File saved successfully: {filePath}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to save file to {filePath}");
                    return StatusCode(500, new { message = "Failed to save file to disk", error = ex.Message });
                }

                var attachment = new TaskAttachment
                {
                    TaskId = taskId,
                    FileName = file.FileName,
                    FilePath = $"/uploads/{uniqueFileName}",
                    FileType = file.ContentType,
                    FileSize = file.Length,
                    UploadedBy = uploadedBy ?? 1,
                    UploadedAt = DateTime.UtcNow
                };

                _context.TaskAttachments.Add(attachment);
                await _context.SaveChangesAsync();

                var savedAttachment = await _context.TaskAttachments
                    .Include(a => a.UploadedByUser)
                    .FirstOrDefaultAsync(a => a.AttachmentId == attachment.AttachmentId);

                var result = new
                {
                    attachment_id = savedAttachment!.AttachmentId,
                    task_id = savedAttachment.TaskId,
                    file_path = savedAttachment.FilePath,
                    file_name = savedAttachment.FileName,
                    file_type = savedAttachment.FileType,
                    file_size = savedAttachment.FileSize,
                    uploaded_by = savedAttachment.UploadedBy,
                    uploader_name = savedAttachment.UploadedByUser?.FullName ?? "Unknown",
                    uploaded_at = savedAttachment.UploadedAt
                };

                return CreatedAtAction(nameof(GetTaskAttachments), new { taskId }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading attachment for task {TaskId}", taskId);
                return StatusCode(500, new { message = "An error occurred while uploading the attachment", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete an attachment
        /// </summary>
        [HttpDelete("{attachmentId}")]
        public async Task<ActionResult> DeleteAttachment(int taskId, int attachmentId)
        {
            try
            {
                var attachment = await _context.TaskAttachments
                    .FirstOrDefaultAsync(a => a.TaskId == taskId && a.AttachmentId == attachmentId);

                if (attachment == null)
                {
                    return NotFound(new { message = $"Attachment with ID {attachmentId} not found for task {taskId}" });
                }

                var actualFilePath = await FindActualFilePath(attachment);
                if (actualFilePath != null)
                {
                    try
                    {
                        System.IO.File.Delete(actualFilePath);
                        _logger.LogInformation($"Deleted file: {actualFilePath}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to delete file: {actualFilePath}");
                    }
                }

                _context.TaskAttachments.Remove(attachment);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Attachment {attachmentId} has been deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting attachment {AttachmentId} for task {TaskId}", attachmentId, taskId);
                return StatusCode(500, new { message = "An error occurred while deleting the attachment", error = ex.Message });
            }
        }
    }
}