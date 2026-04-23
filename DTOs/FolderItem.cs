namespace TestProject.DTOs
{
    public class FolderItem
    {
        public int FileCount { get; set; }
        public int FolderCount { get; set; }
        public DateTime Modified { get; set; }
        public string? Name { get; set; }
    }
}
