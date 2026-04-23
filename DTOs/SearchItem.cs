namespace TestProject.DTOs
{
    public class SearchItem
    {
        public int? FileCount { get; set; }
        public string? FullPath { get; set; }
        public DateTime Modified { get; set; }
        public string? Name { get; set; }
        public long? Size { get; set; }
        public string? Type { get; set; }
    }
}
