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

                // EnumerateFiles splits a pattern containing separators into directory + pattern,
                // which would let the query walk outside the search root.
                if (query.IndexOfAny(InvalidNameChars) >= 0) {
                    return BadRequest(new { message = "Search query cannot contain path characters" });
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
                        // The client controls FileName; keep only the last segment, then validate it.
                        var fileName = Path.GetFileName(file.FileName.Replace('\\', '/'));
                        var filePath = ResolveItemPath(fullPath, fileName);
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
                var filePath = ResolveItemPath(fullPath, file);

                if (!System.IO.File.Exists(filePath)) {
                    return NotFound(new { message = "File not found" });
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                return File(fileBytes, "application/octet-stream", Path.GetFileName(filePath));
            } catch (Exception ex) {
                _logger.LogError(ex, "Error downloading file: {File} from path: {Path}", file, path);
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("delete")]
        public ActionResult Delete([FromQuery] string name, [FromQuery] string path = "", [FromQuery] bool isFolder = false) {
            try {
                var fullPath = ResolvePath(path);
                var itemPath = ResolveItemPath(fullPath, name);

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
                var itemPath = ResolveItemPath(fullFromPath, name);
                var destPath = ResolveItemPath(fullToPath, name);

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
                var itemPath = ResolveItemPath(fullFromPath, name);
                var destPath = ResolveItemPath(fullToPath, name);

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

        [HttpPost("createfolder")]
        public ActionResult CreateFolder([FromQuery] string folderName, [FromQuery] string path = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(folderName))
                {
                    return BadRequest(new { message = "Folder name cannot be empty" });
                }

                var fullPath = ResolvePath(path);
                var newFolderPath = ResolveItemPath(fullPath, folderName);

                if (Directory.Exists(newFolderPath))
                {
                    return BadRequest(new { message = "Folder already exists" });
                }

                Directory.CreateDirectory(newFolderPath);

                return Ok(new { message = "Folder created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder: {FolderName} in path: {Path}", folderName, path);
                return BadRequest(new { message = ex.Message });
            }
        }

        // Windows and macOS file systems are case-insensitive by default; Linux is not, so a
        // case-insensitive comparison there would treat a different directory as "home".
        private static readonly StringComparison PathComparison =
            OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

        private static readonly char[] InvalidNameChars =
            [.. Path.GetInvalidFileNameChars().Union(['/', '\\'])];

        private string HomeRoot => Path.TrimEndingDirectorySeparator(Path.GetFullPath(_homeDirectory));

        /// <summary>
        /// True when <paramref name="fullPath"/> is the home directory or inside it. Compares on a
        /// directory boundary so a sibling such as "/data/home-evil" does not match "/data/home".
        /// </summary>
        private bool IsWithinHome(string fullPath)
        {
            var home = HomeRoot;
            if (fullPath.Equals(home, PathComparison))
            {
                return true;
            }

            var prefix = Path.EndsInDirectorySeparator(home) ? home : home + Path.DirectorySeparatorChar;
            return fullPath.StartsWith(prefix, PathComparison);
        }

        /// <summary>Resolves a directory path relative to the home directory.</summary>
        private string ResolvePath(string? relativePath)
        {
            var home = HomeRoot;
            if (string.IsNullOrWhiteSpace(relativePath) || relativePath == "/")
            {
                return home;
            }

            var resolvedPath = Path.GetFullPath(Path.Combine(home, relativePath.TrimStart('/', '\\')));

            if (!IsWithinHome(resolvedPath))
            {
                throw new UnauthorizedAccessException("Access to this path is not allowed");
            }

            return resolvedPath;
        }

        /// <summary>
        /// Resolves a single file or folder name inside an already-resolved directory. The name must
        /// be one path segment: no separators, no "." or "..", and the result must stay inside home.
        /// </summary>
        private string ResolveItemPath(string directory, string? name)
        {
            if (string.IsNullOrWhiteSpace(name) || name is "." or ".." || name.IndexOfAny(InvalidNameChars) >= 0)
            {
                throw new ArgumentException("Invalid file or folder name");
            }

            var itemPath = Path.GetFullPath(Path.Combine(directory, name));

            // GetFullPath can still collapse odd names (e.g. Windows trims trailing dots), so re-check.
            if (!IsWithinHome(itemPath) || itemPath.Equals(HomeRoot, PathComparison))
            {
                throw new UnauthorizedAccessException("Access to this path is not allowed");
            }

            return itemPath;
        }

        private string GetRelativePath(string fullPath)
        {
            var normalizedHome = HomeRoot;
            if (IsWithinHome(fullPath))
            {
                return fullPath[normalizedHome.Length..].TrimStart('\\', '/').Replace("\\", "/");
            }

            return fullPath.Replace("\\", "/");
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
