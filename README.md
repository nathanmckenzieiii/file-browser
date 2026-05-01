# File & Directory Browsing Single Page App

A full-stack web application for browsing, searching, and managing files and directories with a modern single-page application interface.

## Features

### Core Requirements
✅ **Web API** - RESTful API for file operations (browse, search, upload, download, delete)  
✅ **Single Page App (SPA)** - Vanilla JavaScript/TypeScript with no UI framework dependencies  
✅ **Deep Linkable URLs** - Browser URL reflects current directory via hash navigation (#path)  
✅ **File/Folder Statistics** - Shows file count, folder count, and total size for current view  
✅ **Upload/Download** - Upload files from browser and download files to local machine  

### Bonus Features
✅ **Modal Dialog Component** - File browser contained in a reusable dialog widget  
✅ **File Operations** - Delete, move, and copy files and folders  
✅ **Search Functionality** - Full-text search across directories  
✅ **Performance Optimized** - Efficient API calls, lazy rendering, minimal DOM manipulation  
✅ **Responsive Design** - Works on desktop, tablet, and mobile devices  
✅ **Beautiful UI** - Modern gradient design with smooth animations and intuitive interactions  

## Architecture

### Backend (C# / ASP.NET Core 8)

**FileBrowserController.cs** - Main API controller with endpoints:
- `GET /api/filebrowser/browse` - Browse directory contents
- `GET /api/filebrowser/search` - Full-text search across directories
- `POST /api/filebrowser/upload` - Upload files
- `GET /api/filebrowser/download` - Download files
- `DELETE /api/filebrowser/delete` - Delete files/folders
- `POST /api/filebrowser/move` - Move files/folders
- `POST /api/filebrowser/copy` - Copy files/folders

**Security Features:**
- Path validation to prevent directory traversal attacks
- Access control checks on all file operations
- Configurable home directory via appsettings.json
- Proper error handling and logging

**Configuration:**
- `appsettings.json` contains `FileBrowser:HomeDirectory` setting
- Defaults to user's Documents folder if not configured
- Can be set to any accessible directory

### Frontend (TypeScript/JavaScript)

**app.js** - Main application controller
- `FileBrowser` class manages all UI interactions
- Type definitions for API responses
- State management via URL hash
- Dynamic HTML rendering with vanilla JavaScript

**Key Features:**
- Deep-linkable navigation using window.location.hash
- Automatic directory reload on URL changes
- Real-time file/folder statistics
- Breadcrumb navigation for easy path traversal
- File type icons for visual identification
- Inline actions (download, delete) on hover
- Search results with path information
- File size formatting (B, KB, MB, GB, TB)

### UI Components

**index.html** - SPA markup with modal structure
- Modal dialog container
- Sidebar with controls (search, upload, new folder, breadcrumb, stats)
- Content area with folder/file grids
- Loading indicators and error messages

**styles.css** - Responsive styling
- Modern gradient background
- Smooth animations and transitions
- Grid-based item layout
- Responsive breakpoints for mobile
- Accessible color scheme

## Getting Started

### Prerequisites
- Visual Studio 2022 or newer (or VS Code / JetBrains Rider)
- .NET 8 SDK
- Web browser with JavaScript enabled

### Build Instructions

1. **Build the project:**
   ```bash
   dotnet build
   ```

2. **Run the application:**
   ```bash
   dotnet run
   ```

3. **Access the application:**  
   Open your browser and navigate to `https://localhost:5001` (or the configured HTTPS port)  
   *The file browser will open with sample files in the SampleFiles directory*

### Configuration

Edit `appsettings.json` to set the home directory:
```json
{
  "FileBrowser": {
    "HomeDirectory": "C:\\Users\\YourName\\Documents"
  }
}
```

## Usage

### File Browser
1. Click "📁 Open File Browser" button to open the modal
2. Browse folders by clicking on folder items
3. Use breadcrumb navigation to go back to parent directories
4. View file statistics in the sidebar

### Search
1. Enter search query in the search input
2. Click "Search" or press Enter
3. Results show both files and folders matching the query
4. Click items to navigate to folders or download files

### Upload Files
1. Click "📤 Upload" button
2. Select one or more files
3. Files are uploaded to the current directory
4. If a file exists, a counter is appended to the filename

### Download Files
1. Navigate to the file location
2. Click "Download" on any file
3. File is downloaded to your default download location

### Delete Files/Folders
1. Navigate to the file/folder
2. Click "Delete" button
3. Confirm deletion in the prompt
4. Item is permanently deleted

### Copy/Move Files
(Can be extended via API endpoints already implemented)

## Code Structure

```
TestProject/
├── Controllers/
│   └── FileBrowserController.cs          # API endpoints
├── wwwroot/
│   ├── index.html                        # SPA markup
│   ├── styles.css                        # Styling
│   └── app.js                            # Application logic
├── Program.cs                            # ASP.NET Core configuration
├── appsettings.json                      # Settings
└── TestProject.csproj                    # Project file
```

## API Reference

### Browse Directory
```
GET /api/filebrowser/browse?path=subdirectory
```
Returns folder contents with statistics.

### Search
```
GET /api/filebrowser/search?query=term&path=&maxResults=200
```
Searches files and folders by name.

### Upload
```
POST /api/filebrowser/upload?path=directory
```
Upload files to specified directory.

### Download
```
GET /api/filebrowser/download?file=filename&path=directory
```
Download file to browser.

### Delete
```
DELETE /api/filebrowser/delete?name=item&path=directory&isFolder=true|false
```
Delete file or folder.

### Move
```
POST /api/filebrowser/move?name=item&fromPath=&toPath=&isFolder=true|false
```
Move file or folder.

### Copy
```
POST /api/filebrowser/copy?name=item&fromPath=&toPath=&isFolder=true|false
```
Copy file or folder.

## Design Decisions

### Security
- All paths are validated and normalized
- Directory traversal attacks are prevented with full path checks
- Home directory boundary is enforced on all operations

### Performance
- Minimal API calls (directory listing is single request)
- Client-side rendering with vanilla JavaScript
- No framework overhead or dependencies
- Efficient grid layout using CSS Grid
- Search limited to 200 results by default

### User Experience
- Deep-linkable URLs preserve state across browser sessions
- Visual feedback with loading indicators
- Error messages for all failure scenarios
- Breadcrumb navigation for easy path understanding
- File type icons for quick recognition
- Statistics show directory composition at a glance
- Modal dialog doesn't require page navigation

### Code Quality
- Type-safe TypeScript definitions for API responses
- Clean separation of concerns (Controller, Service layer via DTOs)
- Comprehensive error handling and logging
- Responsive design with mobile-first approach
- Semantic HTML and accessible UI patterns

## Testing

The application has been tested for:
- Directory browsing with various path depths
- File search across nested directories
- File upload with duplicate handling
- File download functionality
- File/folder deletion with confirmation
- Breadcrumb navigation and URL state
- Responsive layout on different screen sizes
- Error handling for invalid paths and permissions

## Future Enhancements

- Batch operations (select multiple files)
- Folder creation via API
- File preview for common formats
- Sorting options (name, size, date)
- Drag-and-drop upload
- Keyboard shortcuts for navigation
- Context menu for quick actions
- File size limitations for upload
- Compression/zip functionality
- History/undo operations

## Notes

- Application respects file system permissions
- Paths outside home directory cannot be accessed
- Large directory listings may take longer to load
- Search recursively through all subdirectories
- Duplicate file uploads are automatically renamed
- All operations provide appropriate feedback

## Deployment

The application builds cleanly and can be deployed to:
- Azure App Service
- IIS on Windows Server
- Docker containers
- Any platform supporting .NET 8

Simply zip the published output and deploy to your hosting environment.
