using Microsoft.AspNetCore.Mvc;
using TestProject.DTOs;

namespace TestProject.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class FileBrowserController(ILogger<FileBrowserController> logger, IConfiguration configuration) : ControllerBase {
        private readonly string _homeDirectory = configuration["FileBrowser:HomeDirectory"] ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents");
        private readonly ILogger<FileBrowserController> _logger = logger;

        [HttpGet("browse")]
        public ActionResult<BrowseResult> Browse([FromQuery] string path = "") {
            try {
                var fullPath = ResolvePath(path);

                if (!Directory.Exists(fullPath)) {
                    return NotFound(new { message = "Directory not found" });
                }

                var directory = new DirectoryInfo(fullPath);
                var files = new List<FileItem>();
                var folders = new List<FolderItem>();

                try {
                    foreach (var file in directory.GetFiles()) {
                        files.Add(new FileItem {
                            Name = file.Name,
                            Size = file.Length,
                            Modified = file.LastWriteTimeUtc
                        });
                    }

                    foreach (var folder in directory.GetDirectories()) {
                        var folderInfo = new DirectoryInfo(folder.FullName);
                        folders.Add(new FolderItem {
                            Name = folder.Name,
                            FileCount = folderInfo.GetFiles().Length,
                            FolderCount = folderInfo.GetDirectories().Length,
                            Modified = folder.LastWriteTimeUtc
                        });
                    }
                } catch (UnauthorizedAccessException ex) {
                    _logger.LogWarning(ex, "Access denied to directory: {FullPath}", fullPath);
                }

                return Ok(new BrowseResult {
                    Path = path,
                    Files = files,
                    Folders = folders,
                    TotalSize = files.Sum(f => f.Size),
                    TotalFiles = files.Count,
                    TotalFolders = folders.Count
                });
            } catch (Exception ex) {
                _logger.LogError(ex, "Error browsing directory: {Path}", path);
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("search")]
        public ActionResult<SearchResult> Search([FromQuery] string query, [FromQuery] string path = "", [FromQuery] int maxResults = 100) {
            try {
                if (string.IsNullOrWhiteSpace(query)) {
                    return BadRequest(new { message = "Search query cannot be empty" });
                }

                var fullPath = ResolvePath(path);

                if (!Directory.Exists(fullPath)) {
                    return NotFound(new { message = "Directory not found" });
                }

                var results = new List<SearchItem>();
                var searchPattern = $"*{query}*";
                var directory = new DirectoryInfo(fullPath);

                try {
                    var matchingFiles = directory.EnumerateFiles(searchPattern, SearchOption.AllDirectories)
                        .Take(maxResults);

                    foreach (var file in matchingFiles) {
                        results.Add(new SearchItem {
                            Name = file.Name,
                            FullPath = GetRelativePath(file.FullName),
                            Type = "file",
                            Size = file.Length,
                            Modified = file.LastWriteTimeUtc
                        });
                    }

                    var matchingFolders = directory.EnumerateDirectories(searchPattern, SearchOption.AllDirectories)
                        .Take(maxResults - results.Count);

                    foreach (var folder in matchingFolders) {
                        var files = folder.GetFiles();
                        results.Add(new SearchItem {
                            Name = folder.Name,
                            FullPath = GetRelativePath(folder.FullName),
                            Type = "folder",
                            FileCount = files.Length,
                            Modified = folder.LastWriteTimeUtc
                        });
                    }
                } catch (UnauthorizedAccessException ex) {
                    _logger.LogWarning(ex, "Access denied during search: {FullPath}", fullPath);
                }

                return Ok(new SearchResult {
                    Query = query,
                    Results = [.. results.OrderBy(r => r.Type).ThenBy(r => r.Name)]
                });
            } catch (Exception ex) {
                _logger.LogError(ex, "Error searching: {Query}", query);
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("upload")]
        public async Task<ActionResult<UploadResult>> Upload([FromQuery] string path = "") {
            try {
                var fullPath = ResolvePath(path);

                if (!Directory.Exists(fullPath)) {
                    return NotFound(new { message = "Directory not found" });
                }

                var files = Request.Form.Files;
                var uploadedFiles = new List<UploadedFile>();

                foreach (var file in files) {
                    if (file.Length > 0) {
                        var filePath = Path.Combine(fullPath, file.FileName);
                        var fileName = Path.GetFileName(filePath);
                        var counter = 1;

                        while (System.IO.File.Exists(filePath)) {
                            var name = Path.GetFileNameWithoutExtension(fileName);
                            var ext = Path.GetExtension(fileName);
                            fileName = $"{name} ({counter}){ext}";
                            filePath = Path.Combine(fullPath, fileName);
                            counter++;
                        }

                        using (var stream = new FileStream(filePath, FileMode.Create)) {
                            await file.CopyToAsync(stream);
                        }

                        uploadedFiles.Add(new UploadedFile {
                            Name = fileName,
                            Size = file.Length
                        });
                    }
                }

                return Ok(new UploadResult {
                    Success = true,
                    Files = uploadedFiles
                });
            } catch (Exception ex) {
                _logger.LogError(ex, "Error uploading file to path: {Path}", path);
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("download")]
        public ActionResult Download([FromQuery] string file, [FromQuery] string path = "") {
            try {
                var fullPath = ResolvePath(path);
                var filePath = Path.Combine(fullPath, file);

                if (!System.IO.File.Exists(filePath)) {
                    return NotFound(new { message = "File not found" });
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                return File(fileBytes, "application/octet-stream", file);
            } catch (Exception ex) {
                _logger.LogError(ex, "Error downloading file: {File} from path: {Path}", file, path);
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("delete")]
        public ActionResult Delete([FromQuery] string name, [FromQuery] string path = "", [FromQuery] bool isFolder = false) {
            try {
                var fullPath = ResolvePath(path);
                var itemPath = Path.Combine(fullPath, name);

                if (isFolder) {
                    if (!Directory.Exists(itemPath)) {
                        return NotFound(new { message = "Folder not found" });
                    }

                    Directory.Delete(itemPath, recursive: true);
                } else {
                    if (!System.IO.File.Exists(itemPath)) {
                        return NotFound(new { message = "File not found" });
                    }

                    System.IO.File.Delete(itemPath);
                }

                return Ok(new { message = "Item deleted successfully" });
            } catch (Exception ex) {
                _logger.LogError(ex, "Error deleting item: {Name} in path: {Path}", name, path);
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("move")]
        public ActionResult Move([FromQuery] string name, [FromQuery] string fromPath = "", [FromQuery] string toPath = "", [FromQuery] bool isFolder = false) {
            try {
                var fullFromPath = ResolvePath(fromPath);
                var fullToPath = ResolvePath(toPath);
                var itemPath = Path.Combine(fullFromPath, name);
                var destPath = Path.Combine(fullToPath, name);

                if (isFolder) {
                    if (!Directory.Exists(itemPath)) {
                        return NotFound(new { message = "Folder not found" });
                    }

                    Directory.Move(itemPath, destPath);
                } else {
                    if (!System.IO.File.Exists(itemPath)) {
                        return NotFound(new { message = "File not found" });
                    }

                    System.IO.File.Move(itemPath, destPath);
                }

                return Ok(new { message = "Item moved successfully" });
            } catch (Exception ex) {
                _logger.LogError(ex, "Error moving item: {Name} from {FromPath} to {ToPath}", name, fromPath, toPath);
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("copy")]
        public ActionResult Copy([FromQuery] string name, [FromQuery] string fromPath = "", [FromQuery] string toPath = "", [FromQuery] bool isFolder = false) {
            try {
                var fullFromPath = ResolvePath(fromPath);
                var fullToPath = ResolvePath(toPath);
                var itemPath = Path.Combine(fullFromPath, name);
                var destPath = Path.Combine(fullToPath, name);

                if (isFolder) {
                    if (!Directory.Exists(itemPath)) {
                        return NotFound(new { message = "Folder not found" });
                    }

                    CopyDirectory(itemPath, destPath);
                } else {
                    if (!System.IO.File.Exists(itemPath)) {
                        return NotFound(new { message = "File not found" });
                    }

                    System.IO.File.Copy(itemPath, destPath, overwrite: true);
                }

                return Ok(new { message = "Item copied successfully" });
            } catch (Exception ex) {
                _logger.LogError(ex, "Error copying item: {Name} from {FromPath} to {ToPath}", name, fromPath, toPath);
                return BadRequest(new { message = ex.Message });
            }
        }

        private string ResolvePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || relativePath == "/")
            {
                return Path.GetFullPath(_homeDirectory);
            }

            var fullPath = Path.Combine(_homeDirectory, relativePath.TrimStart('/'));
            var resolvedPath = Path.GetFullPath(fullPath);
            var normalizedHome = Path.GetFullPath(_homeDirectory);

            if (!resolvedPath.StartsWith(normalizedHome, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Access to this path is not allowed");
            }

            return resolvedPath;
        }

        private string GetRelativePath(string fullPath)
        {
            var normalizedHome = Path.GetFullPath(_homeDirectory);
            if (fullPath.StartsWith(normalizedHome, StringComparison.OrdinalIgnoreCase))
            {
                return fullPath[normalizedHome.Length..].TrimStart('\\', '/');
            }

            return fullPath;
        }

        private static void CopyDirectory(string source, string destination) {
            var sourceInfo = new DirectoryInfo(source);

            if (!Directory.Exists(destination)) {
                Directory.CreateDirectory(destination);
            }

            foreach (var file in sourceInfo.GetFiles()) {
                file.CopyTo(Path.Combine(destination, file.Name), true);
            }

            foreach (var dir in sourceInfo.GetDirectories()) {
                CopyDirectory(dir.FullName, Path.Combine(destination, dir.Name));
            }
        }
    }
}
