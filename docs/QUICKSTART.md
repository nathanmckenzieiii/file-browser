# Quick Start Guide

## 5-Minute Setup

### Prerequisites
- .NET 8 SDK installed
- Visual Studio 2022+, VS Code, or Rider
- A web browser (Chrome, Firefox, Safari, Edge)

### Step 1: Open the Project
```bash
# Navigate to project directory
cd C:\Users\Natha\source\repos\MapLarge\TestProject_2025\TestProject\

# Open in Visual Studio, or build from command line
dotnet build
```

### Step 2: Run the Application
```bash
# From project directory
dotnet run

# Or press F5 in Visual Studio
```

### Step 3: Access the Application
Open your browser to:
```
https://localhost:7146
```

(The port number may vary based on your launchSettings.json - check the console output for the actual URL)

## First Steps

1. **Click the button**: Click "📁 Open File Browser" to open the modal
2. **Browse folders**: Click on any folder to enter it
3. **View files**: See files and folders in the current directory
4. **Check stats**: View file count, folder count, and total size in the sidebar
5. **Navigate back**: Click "Home" in the breadcrumb or use browser back button

## Try These Features

### Search Files
1. Enter a search term in the search box (e.g., "test")
2. Click "Search" or press Enter
3. See results from all subdirectories

### Upload a File
1. Click "📤 Upload"
2. Select any file from your computer
3. Watch it appear in the file list

### Download a File
1. Locate any file in the browser
2. Click "Download" on the file
3. File appears in your Downloads folder

### Navigate with Deep Links
1. Browse to a folder
2. Copy the URL from the address bar
3. Share it or open in a new tab
4. The app remembers your location!

### Delete a File (Use Caution!)
1. Hover over a file
2. Click "Delete" button
3. Confirm deletion
4. File is removed

## Configuration

### Change the Home Directory

Edit `appsettings.json`:

```json
{
  "FileBrowser": {
    "HomeDirectory": "C:\\Users\\YourName\\Documents"
  }
}
```

**Leave empty** to use the default user Documents folder.

### Common Configurations

**Desktop Development:**
```json
{
  "FileBrowser": {
    "HomeDirectory": "C:\\Users\\YourName"
  }
}
```

**Server Deployment:**
```json
{
  "FileBrowser": {
    "HomeDirectory": "/var/www/files"
  }
}
```

**Testing:**
```json
{
  "FileBrowser": {
    "HomeDirectory": "C:\\TestFolder"
  }
}
```

## Troubleshooting

### Modal doesn't appear
- Ensure JavaScript is enabled in your browser
- Check browser console (F12) for errors
- Hard refresh the page (Ctrl+Shift+R)

### Files not showing
- Ensure the `HomeDirectory` is set and accessible
- Check that the folder actually exists
- Try restarting the application

### Upload fails
- Ensure the directory is writable
- Check file size (limit may vary)
- Try a smaller file first

### Search returns no results
- Search is case-insensitive and partial match
- Search includes both files and folders
- May take longer in deeply nested structures

### Permission denied errors
- Some folders may not be accessible due to OS permissions
- The app can't browse restricted system folders
- Check your Windows/Linux file permissions

## URL Structure

The application uses hash-based URLs:

```
https://localhost:7146/#path/to/folder
https://localhost:7146/#Documents/Projects/2025
https://localhost:7146/#  (root/home)
```

This means:
- You can bookmark specific folders
- Browser back/forward work correctly
- URLs are shareable (same system)

## Performance Tips

1. **Large directories**: Search results are limited to first 200 matches
2. **Nested folders**: Very deeply nested structures may load slower
3. **Large files**: Upload/download speed depends on file size
4. **Network**: Works best on local network or same machine

## File Type Icons

The app shows different icons based on file type:

- 📁 Folders
- 📄 Text documents, PDFs
- 📝 Word documents (doc, docx, txt)
- 📊 Spreadsheets (xls, xlsx, csv)
- 🖼️ Images (jpg, png, gif)
- 🎵 Audio files (mp3)
- 🎬 Video files (mp4, avi)
- 📦 Archives (zip, rar, 7z)
- ⚙️ Programs/DLLs (exe, dll)
- ✨ Code files (js, ts)
- 🎨 Web files (css)
- 🌐 Web documents (html)
- 📄 Everything else

## Mobile Usage

The app is responsive and works on mobile:

1. Open URL on mobile device
2. Portrait or landscape mode
3. All features work the same
4. Touch-friendly buttons

**Note**: Uploading from mobile varies by browser and OS.

## Deployment

### Build for Production
```bash
dotnet publish -c Release
```

Output in `bin/Release/net8.0/publish/`

### Deploy to Azure
```bash
az webapp up --resource-group mygroup --name myapp --sku B1
```

### Deploy with Docker
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY . .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "TestProject.dll"]
```

## Getting Help

### Check Application Logs
Look in Visual Studio Output window (Debug pane) during development.

### Browser Console
Press F12 to open developer tools. Errors appear in the Console tab.

### Review Documentation
- `README.md` - Complete feature documentation
- `DEVELOPMENT.md` - Architecture and design decisions
- `IMPLEMENTATION.md` - Advanced implementation details

## Next Steps

### Explore the Code
1. Open `Controllers/FileBrowserController.cs` - API endpoints
2. Open `wwwroot/app.js` - Frontend logic
3. Open `wwwroot/styles.css` - Styling
4. Open `wwwroot/index.html` - Page structure

### Modify and Extend
Add new features:
- File preview
- Batch operations
- Folder creation
- Sorting options
- Drag-and-drop

### Test Scenarios
1. Navigate to nested folders
2. Search for files with various patterns
3. Upload different file types
4. Delete and recreate files
5. Test on mobile devices

## Common Questions

**Q:** Can I change the home directory path?  
**A:** Yes, edit `appsettings.json` and update `FileBrowser:HomeDirectory`

**Q:** Can I upload any file type?  
**A:** Yes, except for system restrictions and file size limits

**Q:** Is the search case-sensitive?  
**A:** No, search is case-insensitive

**Q:** Can I access files outside the home directory?  
**A:** No, by design for security. Configure home directory to include needed paths.

**Q:** Does it work without internet?  
**A:** Yes, it's a local web app. Works completely offline once loaded.

**Q:** Can multiple people use it simultaneously?  
**A:** Yes, but they share the same file system and could conflict if editing same files.

**Q:** Is there a file size limit?  
**A:** Upload limit depends on server configuration (default ~100MB)

**Q:** Can I use this on a Mac/Linux?  
**A:** Yes! .NET 8 supports macOS and Linux. File paths use forward slashes.

## Support

For issues or questions:
1. Check the browser console for error messages
2. Review application logs in Visual Studio
3. Verify file paths and permissions
4. Rebuild the solution
5. Restart the application

## Tips for Best Experience

1. Use Chrome or Edge for best performance
2. Bookmark frequently used folders
3. Use search for large directories
4. Keep folder structures reasonable (don't go 50 levels deep)
5. Use the breadcrumb to navigate quickly
