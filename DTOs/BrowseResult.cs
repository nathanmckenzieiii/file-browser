namespace TestProject.DTOs {
    public class BrowseResult {
        public List<FileItem>? Files { get; set; }
        public List<FolderItem>? Folders { get; set; }
        public string? Path { get; set; }
        public int TotalFiles { get; set; }
        public int TotalFolders { get; set; }
        public long TotalSize { get; set; }
    }
}
