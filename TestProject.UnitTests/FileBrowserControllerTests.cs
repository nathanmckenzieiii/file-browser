using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TestProject.Controllers;
using TestProject.DTOs;

namespace TestProject.UnitTests
{
    public class FileBrowserControllerTests : IDisposable
    {
        private readonly Mock<ILogger<FileBrowserController>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly FileBrowserController _controller;
        private readonly string _testDirectory;

        public FileBrowserControllerTests()
        {
            _mockLogger = new Mock<ILogger<FileBrowserController>>();
            _mockConfiguration = new Mock<IConfiguration>();

            // Use a unique temp directory for each test run
            _testDirectory = Path.Combine(Path.GetTempPath(), $"TestFileBrowser_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);

            _mockConfiguration
                .Setup(x => x["FileBrowser:HomeDirectory"])
                .Returns(_testDirectory);

            _controller = new FileBrowserController(_mockLogger.Object, _mockConfiguration.Object);
        }

        [Fact]
        public void Browse_AttemptsPathTraversal_ReturnsBadRequest()
        {
            // Attempt to escape the home directory with ../
            var result = _controller.Browse("../../../etc/passwd");

            // Should fail gracefully, not grant access
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequest.Value);
        }

        [Fact]
        public void Browse_ValidDirectoryWithContent_ReturnsBrowseResultWithCorrectCounts()
        {
            // Arrange: Create test files and folders
            File.WriteAllText(Path.Combine(_testDirectory, "file1.txt"), "content");
            File.WriteAllText(Path.Combine(_testDirectory, "file2.txt"), "content");
            Directory.CreateDirectory(Path.Combine(_testDirectory, "subfolder1"));
            Directory.CreateDirectory(Path.Combine(_testDirectory, "subfolder2"));

            // Act
            var result = _controller.Browse("");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var browseResult = Assert.IsType<BrowseResult>(okResult.Value);

            Assert.Equal(2, browseResult.TotalFiles);
            Assert.Equal(2, browseResult.TotalFolders);
            Assert.Equal("file1.txt", browseResult.Files[0].Name);
            Assert.Equal("subfolder1", browseResult.Folders[0].Name);
        }

        [Fact]
        public void Search_ValidQuery_ReturnsMatchingFilesAndFolders()
        {
            // Arrange: Create files with searchable names
            File.WriteAllText(Path.Combine(_testDirectory, "document.pdf"), "");
            File.WriteAllText(Path.Combine(_testDirectory, "report.txt"), "");
            Directory.CreateDirectory(Path.Combine(_testDirectory, "documents"));

            // Act
            var result = _controller.Search("doc", "");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var searchResult = Assert.IsType<SearchResult>(okResult.Value);

            Assert.Equal(2, searchResult.Results.Count); // document.pdf and documents folder
            Assert.Contains(searchResult.Results, r => r.Name == "document.pdf");
            Assert.Contains(searchResult.Results, r => r.Name == "documents" && r.Type == "folder");
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }

            GC.SuppressFinalize(this);
        }
    }
}