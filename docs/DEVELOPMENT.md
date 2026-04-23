# Development Guide

## Project Overview

This File Browser application is designed to showcase full-stack development skills with a focus on:
- **Clean Code**: Clear separation of concerns, minimal boilerplate
- **Security**: Input validation, path security, permission handling
- **Performance**: Efficient API design, client-side rendering
- **User Experience**: Deep-linkable URLs, intuitive UI, responsive design

## Architecture

### Three-Layer Design

**1. API Layer (Controllers/FileBrowserController.cs)**
- RESTful endpoints for all file operations
- Input validation and error handling
- Path security checks to prevent traversal attacks
- Logging for diagnostics

**2. Data Transfer Objects (DTOs)**
- `BrowseResult`, `FileItemDto`, `FolderItemDto` - for directory browsing
- `SearchResult`, `SearchItemDto` - for search operations
- `UploadResult`, `UploadedFileDto` - for uploads

**3. Presentation Layer (wwwroot/app.js)**
- `FileBrowser` class encapsulates all UI logic
- Type definitions for API contracts
- Event-driven architecture
- DOM manipulation with vanilla JavaScript

## Key Implementation Details

### Path Security

The `ResolvePath` method is critical for security:
```csharp
private string ResolvePath(string relativePath) {
    var fullPath = Path.Combine(_homeDirectory, relativePath.TrimStart('/'));
    var resolvedPath = Path.GetFullPath(fullPath);

    // Prevents directory traversal attacks
    if (!resolvedPath.StartsWith(_homeDirectory)) {
        throw new UnauthorizedAccessException(...);
    }
    return resolvedPath;
}
```

This ensures users cannot access files outside the configured home directory.

### Deep-Linkable URLs

State is maintained in the URL hash:
```javascript
private updateUrl(): void {
    window.location.hash = this.currentPath;
}

private loadFromUrl(): void {
    const hash = window.location.hash.slice(1);
    this.currentPath = hash || "";
}
```

Benefits:
- Browser back/forward buttons work correctly
- State is shareable via URL
- Bookmarking works as expected
- Page refresh maintains position

### File Type Detection

Icons are assigned based on file extensions:
```javascript
private getFileIcon(fileName: string): string {
    const ext = fileName.split(".").pop()?.toLowerCase() || "";
    const iconMap: { [key: string]: string } = {
        "pdf": "📄",
        "doc": "📝", // ... more types
    };
    return iconMap[ext] || "📄";
}
```

This provides visual feedback without relying on file extensions for security.

### Responsive Grid Layout

CSS Grid automatically adapts to screen size:
```css
.items-container {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
    gap: 15px;
}
```

- Desktop: Multiple columns
- Tablet: 2-3 columns
- Mobile: 1-2 columns

## API Design

### Browse Endpoint
```
GET /api/filebrowser/browse?path=folder/subfolder

Response:
{
  "path": "folder/subfolder",
  "files": [...],
  "folders": [...],
  "totalSize": 1024000,
  "totalFiles": 5,
  "totalFolders": 2
}
```

Key design choice: Separate files and folders for easier client-side rendering.

### Search Endpoint
```
GET /api/filebrowser/search?query=test&path=&maxResults=200

Response:
{
  "query": "test",
  "results": [
    {
      "name": "test.txt",
      "fullPath": "documents/test.txt",
      "type": "file",
      "size": 1024,
      "modified": "2024-01-01T00:00:00Z"
    }
  ]
}
```

Key design choice: Results include full path so users know file location without navigation.

### Upload Endpoint
```
POST /api/filebrowser/upload?path=folder

multipart/form-data:
  files: [file1, file2, ...]

Response:
{
  "success": true,
  "files": [
    {"name": "file1.txt", "size": 1024}
  ]
}
```

Key design choice: 
- Supports multiple files
- Automatic duplicate handling (appends counter)
- Returns uploaded file info for validation

## Error Handling

### Server-Side (C#)
```csharp
try {
    // Operation
} catch (UnauthorizedAccessException ex) {
    _logger.LogWarning($"Access denied: {ex.Message}", ex);
    return StatusCode(403, new { message = "Access denied" });
} catch (Exception ex) {
    _logger.LogError($"Error: {ex.Message}", ex);
    return BadRequest(new { message = ex.Message });
}
```

### Client-Side (JavaScript)
```javascript
try {
    const response = await fetch(url);
    if (!response.ok) {
        throw new Error("Operation failed");
    }
    // Handle success
} catch (error) {
    this.showError(error.message);
}
```

## Performance Considerations

1. **API Efficiency**
   - Single API call per directory navigation
   - Search limited to prevent large result sets
   - No unnecessary requests

2. **Client-Side Rendering**
   - DOM created only when needed
   - Grid layout is efficient for many items
   - No framework overhead

3. **Caching Opportunities**
   - Could cache directory listings
   - Could implement virtual scrolling for large directories
   - Could lazy-load search results

## Testing Scenarios

### Functional Testing
1. Navigate to various directory depths
2. Test search with special characters
3. Upload files with duplicate names
4. Delete files and folders
5. Test breadcrumb navigation
6. Test URL history (back/forward)

### Edge Cases
1. Empty directories
2. Large numbers of files (1000+)
3. Long file names
4. Special characters in names
5. Permission-denied scenarios
6. Disk space issues

### Security Testing
1. Attempt directory traversal (../)
2. Attempt to access restricted directories
3. Upload malicious file types
4. Test URL tampering

## Extension Points

### Add New File Operations
To add a new operation (e.g., rename):

1. **Controller:**
```csharp
[HttpPost("rename")]
public ActionResult Rename([FromQuery] string oldName, [FromQuery] string newName, [FromQuery] string path = "") {
    // Implementation
}
```

2. **Frontend:**
```javascript
private async renameItem(oldName: string, newName: string): Promise<void> {
    const url = `${this.apiBaseUrl}/rename?oldName=...&newName=...&path=...`;
    const response = await fetch(url, { method: "POST" });
    // Handle response
}
```

### Add File Preview
Extend search results to include preview capability for common file types.

### Add Sorting
Add query parameter to browse/search endpoints for sorting options.

## Configuration Best Practices

### Development
```json
{
  "FileBrowser": {
    "HomeDirectory": "C:\\TestDirectory"
  }
}
```

### Production
```json
{
  "FileBrowser": {
    "HomeDirectory": "/data/files"
  }
}
```

Use environment-specific appsettings:
- `appsettings.json` - defaults
- `appsettings.Development.json` - dev overrides
- `appsettings.Production.json` - production overrides

## Monitoring & Logging

The controller logs important events:
- Access denied attempts
- File operations
- Search operations
- Upload/download activity

View logs in:
- Visual Studio Output window during debugging
- Application logs in production

## Browser Compatibility

Tested and working in:
- Chrome/Edge (latest)
- Firefox (latest)
- Safari (latest)

Features used:
- Fetch API
- ES6+ JavaScript
- CSS Grid
- Flexbox

## Code Quality Metrics

- **Lines of Code**: ~1000 (excluding comments)
- **API Endpoints**: 7
- **UI Components**: 1 (FileBrowser class)
- **No External Dependencies**: ✓
- **Type Safety**: TypeScript definitions for all API types
- **Error Handling**: Comprehensive try-catch blocks
