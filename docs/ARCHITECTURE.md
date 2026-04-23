# Architecture & Flow Diagram

## System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      Web Browser                            │
│  ┌───────────────────────────────────────────────────────┐  │
│  │                   SPA (Single Page App)               │  │
│  │  ┌─────────────────────────────────────────────────┐  │  │
│  │  │  FileBrowser Class (app.js - 400 lines)         │  │  │
│  │  │  - UI State Management                          │  │  │
│  │  │  - DOM Rendering                                │  │  │
│  │  │  - Event Handling                               │  │  │
│  │  │  - API Communication                            │  │  │
│  │  │  - URL Navigation (#path)                       │  │  │
│  │  └─────────────────────────────────────────────────┘  │  │
│  │                                                       │  │
│  │  ┌─────────────────────────────────────────────────┐  │  │
│  │  │  UI Components (Styled with CSS - 400 lines)    │  │  │
│  │  │  - Modal Dialog                                 │  │  │
│  │  │  - File/Folder Grid                             │  │  │
│  │  │  - Breadcrumb Navigation                        │  │  │
│  │  │  - Search Interface                             │  │  │
│  │  │  - Upload Controls                              │  │  │
│  │  │  - Statistics Panel                             │  │  │
│  │  └─────────────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                           ↕
                      Fetch API
                    JSON/HTTP REST
                           ↕
┌─────────────────────────────────────────────────────────────┐
│              ASP.NET Core 8 Web Server                      │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  FileBrowserController (350 lines)                    │  │
│  │                                                       │  │
│  │  GET    /api/filebrowser/browse  ────┐                │  │
│  │  GET    /api/filebrowser/search  ────┤                │  │
│  │  POST   /api/filebrowser/upload  ────┼→ File System   │  │
│  │  GET    /api/filebrowser/download────┤                │  │
│  │  DELETE /api/filebrowser/delete  ────┤                │  │
│  │  POST   /api/filebrowser/move    ────┤                │  │
│  │  POST   /api/filebrowser/copy    ────┘                │  │
│  │                                                       │  │
│  │  Features:                                            │  │
│  │  - Path Validation & Security                         │  │
│  │  - Access Control                                     │  │
│  │  - Error Handling & Logging                           │  │
│  │  - Dependency Injection                               │  │
│  │  - Type-safe DTOs                                     │  │
│  └───────────────────────────────────────────────────────┘  │
│                           ↕                                 │
│                   File System (Disk)                        │
│             (Controlled via appsettings.json)               │
└─────────────────────────────────────────────────────────────┘
```

## Request/Response Flow

### Browse Directory Flow

```
User clicks "Open File Browser"
           ↓
Modal dialog opens
           ↓
loadDirectory() called
           ↓
GET /api/filebrowser/browse?path=...
           ↓
API validates path (security check)
           ↓
File system enumeration
           ↓
Response: BrowseResult with files/folders/stats
           ↓
Frontend renders DOM
           ↓
URL updated with hash (#path/to/folder)
           ↓
User sees file/folder grid with stats
```

### Search Flow

```
User types search term
           ↓
User clicks "Search" or presses Enter
           ↓
GET /api/filebrowser/search?query=term&path=...
           ↓
API recursively searches directory
           ↓
Limits results to 200 (performance)
           ↓
Response: SearchResult with matching items
           ↓
Frontend renders search results
           ↓
User can click results to navigate
```

### Upload Flow

```
User clicks "Upload"
           ↓
File dialog opens
           ↓
User selects file(s)
           ↓
FormData created with files
           ↓
POST /api/filebrowser/upload?path=...
           ↓
API writes files to directory
           ↓
If duplicate: appends (1), (2), etc.
           ↓
Response: UploadResult with file info
           ↓
Directory is refreshed
           ↓
Uploaded files appear in grid
```

## Component Structure

### Frontend Components

```
FileBrowser (Main Class)
├── Modal Overlay
│   ├── Modal Header
│   │   ├── Title
│   │   └── Close Button
│   ├── Modal Body
│   │   ├── Sidebar
│   │   │   ├── Search Section
│   │   │   ├── Upload/NewFolder Section
│   │   │   ├── Breadcrumb Navigation
│   │   │   └── Statistics Panel
│   │   └── Content Area
│   │       ├── Folder Grid
│   │       ├── File Grid
│   │       ├── Search Results Grid
│   │       └── Loading/Error Display
│   └── Modal Backdrop
└── Trigger Button
    └── "📁 Open File Browser"
```

### Backend Components

```
FileBrowserController
├── Browse(path)
│   └── Enumerates directory
│       └── Returns folders and files
├── Search(query, path)
│   └── Recursively searches
│       └── Returns matching items
├── Upload(path, files)
│   └── Saves files
│       └── Returns upload results
├── Download(file, path)
│   └── Streams file
│       └── Returns binary data
├── Delete(name, path, isFolder)
│   └── Removes file/folder
│       └── Returns status
├── Move(name, fromPath, toPath, isFolder)
│   └── Relocates item
│       └── Returns status
└── Copy(name, fromPath, toPath, isFolder)
    └── Duplicates item
        └── Returns status

Helper Methods:
├── ResolvePath() - Validates and normalizes paths
├── GetRelativePath() - Converts absolute to relative
└── CopyDirectory() - Recursive folder copy
```

## Data Flow Diagram

```
┌─────────────────┐
│  User Action    │
│  (Browse/etc)   │
└────────┬────────┘
         │
         ↓
┌─────────────────────────┐
│ FileBrowser.method()    │
│ (e.g., loadDirectory()) │
└────────┬────────────────┘
         │
         ↓
┌──────────────────────────┐
│ Fetch API Call           │
│ GET /api/filebrowser/... │
└────────┬─────────────────┘
         │
         ↓
┌────────────────────────────┐
│ API Endpoint Handler       │
│ (Browse/Search/etc)        │
└────────┬───────────────────┘
         │
         ↓
┌────────────────────────────┐
│ Path Validation            │
│ (Security Check)           │
└────────┬───────────────────┘
         │
    ┌────┴─────┐
    │ Valid?    │
    └─────┬────┘
    Yes   │   No
       ┌──┴──┐
       │     │
       ↓     ↓
    Success Error
       │     │
       ↓     ↓
┌────────────────────────────┐
│ File System Operation      │
│ (Enumerate/Search/etc)     │
└────────┬───────────────────┘
         │
         ↓
┌────────────────────────────┐
│ Response Creation          │
│ (DTO Population)           │
└────────┬───────────────────┘
         │
         ↓
┌────────────────────────────┐
│ JSON Response              │
│ (Sent to Frontend)         │
└────────┬───────────────────┘
         │
         ↓
┌────────────────────────────┐
│ Response Handling          │
│ (Frontend JS)              │
└────────┬───────────────────┘
         │
         ↓
┌────────────────────────────┐
│ DOM Rendering              │
│ (Dynamic HTML Creation)    │
└────────┬───────────────────┘
         │
         ↓
┌────────────────────────────┐
│ URL Update                 │
│ (Hash Navigation)          │
└────────┬───────────────────┘
         │
         ↓
┌────────────────────────────┐
│ User Sees Result           │
│ (Browser Display)          │
└────────────────────────────┘
```

## Type Flow

```
API Response (JSON)
           ↓
TypeScript Interface
           ↓
Compile-time Type Checking
           ↓
Type-safe Variable Usage
           ↓
Error Detection at Build Time
```

Example:
```javascript
// TypeScript definitions provide safety
interface BrowseResult {
    path: string;
    files: FileItem[];           // Compiler ensures correct structure
    folders: FolderItemDto[];
    totalSize: number;
}

const data: BrowseResult = await response.json();
// Compiler error if JSON doesn't match interface!
```

## Security Flow

```
User Request
     ↓
Input Validation
├─ Is path empty/null?
└─ Is path string?
     ↓
Path Resolution
├─ Combine with home directory
├─ Normalize path (.., ., etc)
└─ Get full path
     ↓
Security Check
├─ Does resolved path start with home directory?
├─ If NO → UnauthorizedAccessException
└─ If YES → Proceed
     ↓
File System Operation
├─ Try operation
├─ Catch UnauthorizedAccessException
├─ Catch IOException
└─ Catch Generic Exception
     ↓
Response
├─ Success: Return results
└─ Failure: Return error message
```

## Performance Optimization Strategy

```
User Opens File Browser
           ↓
Single API Call to Browse
(Not multiple calls per file)
           ↓
Lazy File Enumeration
(Don't load entire directory into memory)
           ↓
Direct DOM Rendering
(No framework overhead)
           ↓
CSS Grid Auto-Layout
(Responsive without JS)
           ↓
Result: Fast, responsive UI
```

## State Management

```
┌─────────────────────────────────────────┐
│         URL (Source of Truth)           │
│  https://localhost:5001/#Documents/Pro  │
└─────────────────────────────────────────┘
              ↕
┌─────────────────────────────────────────┐
│    FileBrowser.currentPath Variable     │
│            = "Documents/Pro"            │
└─────────────────────────────────────────┘
              ↕
┌─────────────────────────────────────────┐
│      API Request with currentPath       │
│   GET /api/filebrowser/browse?path=...  │
└─────────────────────────────────────────┘
              ↕
┌─────────────────────────────────────────┐
│         Rendered UI Components          │
│    - File Grid                          │
│    - Breadcrumb                         │
│    - Statistics                         │
└─────────────────────────────────────────┘
```

## Responsive Breakpoints

```
┌─────────────────────────────────────────┐
│  Desktop (1920px+)                      │
│  ┌──────────────────────────────────┐   │
│  │ Modal: 90% width, 90% height     │   │
│  │ Sidebar: 280px (fixed)           │   │
│  │ Grid: 8-10 columns               │   │
│  │ Layout: Horizontal (side-by-side)│   │
│  └──────────────────────────────────┘   │
└─────────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────────┐
│  Tablet (768px)                         │
│  ┌──────────────────────────────────┐   │
│  │ Modal: 95% width, 95% height     │   │
│  │ Sidebar: 100% width, 200px height│   │
│  │ Grid: 4-6 columns                │   │
│  │ Layout: Vertical (stacked)       │   │
│  └──────────────────────────────────┘   │
└─────────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────────┐
│  Mobile (480px)                         │
│  ┌──────────────────────────────────┐   │
│  │ Modal: 95% width, 95% height     │   │
│  │ Sidebar: 100% width, 200px height│   │
│  │ Grid: 2-3 columns                │   │
│  │ Layout: Vertical (stacked)       │   │
│  └──────────────────────────────────┘   │
└─────────────────────────────────────────┘
```

## File Structure

```
Project Root
│
├── Controllers/
│   └── FileBrowserController.cs (350 lines)
│       └── 7 API Endpoints + Helper Methods
│
├── wwwroot/ (Static Files)
│   ├── index.html (80 lines)
│   │   └── SPA Structure
│   ├── app.js (400 lines)
│   │   └── FileBrowser Class + Logic
│   └── styles.css (400 lines)
│       └── UI Styling + Responsive
│
├── Configuration
│   ├── Program.cs (Updated)
│   ├── appsettings.json (Updated)
│   └── TestProject.csproj
│
└── Documentation (2000+ lines)
    ├── INDEX.md (Navigation)
    ├── QUICKSTART.md (5-min setup)
    ├── README.md (Complete guide)
    ├── DEVELOPMENT.md (Architecture)
    ├── IMPLEMENTATION.md (Advanced)
    ├── PROJECT_SUMMARY.md (Overview)
    ├── COMPLETION_CHECKLIST.md (Status)
    └── ARCHITECTURE.md (This file)
```

---

## Legend

```
┌─────┐   = Component/Container
│     │
└─────┘

   ↓     = Process Flow / Data Flow
   ↕     = Bidirectional Flow

──→      = Single Direction Flow

──┬──    = Branching/Decision
   ├──
   └──
```

This architecture diagram shows:
1. How the frontend and backend communicate
2. The flow of data through the application
3. How components are organized
4. Security boundaries and validation
5. Responsive design breakpoints
6. State management through URLs

For detailed implementation, see IMPLEMENTATION.md
For usage guide, see README.md
For quick start, see QUICKSTART.md
