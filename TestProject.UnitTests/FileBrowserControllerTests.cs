using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
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

        // Layout for every test:
        //   <root>/home/          <- configured home directory
        //   <root>/home-evil/     <- sibling whose name starts with "home"
        //   <root>/secret.txt     <- file outside home that must stay unreachable
        private readonly string _rootDirectory;
        private readonly string _testDirectory;
        private readonly string _siblingDirectory;
        private readonly string _secretFile;

        public FileBrowserControllerTests()
        {
            _mockLogger = new Mock<ILogger<FileBrowserController>>();
            _mockConfiguration = new Mock<IConfiguration>();

            // Use a unique temp directory for each test run
            _rootDirectory = Path.Combine(Path.GetTempPath(), $"TestFileBrowser_{Guid.NewGuid()}");
            _testDirectory = Path.Combine(_rootDirectory, "home");
            _siblingDirectory = Path.Combine(_rootDirectory, "home-evil");
            _secretFile = Path.Combine(_rootDirectory, "secret.txt");

            Directory.CreateDirectory(_testDirectory);
            Directory.CreateDirectory(_siblingDirectory);
            File.WriteAllText(_secretFile, "top secret");

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
        public void Browse_SiblingDirectorySharingHomePrefix_ReturnsBadRequest()
        {
            File.WriteAllText(Path.Combine(_siblingDirectory, "leaked.txt"), "x");

            var result = _controller.Browse("../home-evil");

            Assert.IsType<BadRequestObjectResult>(result.Result);
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
            Assert.Contains(browseResult.Files!, f => f.Name == "file1.txt");
            Assert.Contains(browseResult.Folders!, f => f.Name == "subfolder1");
        }

        [Fact]
        public void Browse_Subfolder_ReturnsOk()
        {
            Directory.CreateDirectory(Path.Combine(_testDirectory, "sub", "deeper"));

            var result = _controller.Browse("sub/deeper/");

            Assert.IsType<OkObjectResult>(result.Result);
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

            Assert.Equal(2, searchResult.Results!.Count); // document.pdf and documents folder
            Assert.Contains(searchResult.Results, r => r.Name == "document.pdf");
            Assert.Contains(searchResult.Results, r => r.Name == "documents" && r.Type == "folder");
        }

        [Theory]
        [InlineData("/../")]
        [InlineData("../secret")]
        [InlineData("..\\secret")]
        public void Search_QueryWithPathCharacters_ReturnsBadRequestAndLeaksNothing(string query)
        {
            var result = _controller.Search(query, "");

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public void Download_ValidFile_ReturnsContent()
        {
            File.WriteAllText(Path.Combine(_testDirectory, "hello.txt"), "hello");

            var result = _controller.Download("hello.txt", "");

            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("hello", Encoding.UTF8.GetString(fileResult.FileContents));
            Assert.Equal("hello.txt", fileResult.FileDownloadName);
        }

        [Theory]
        [InlineData("../secret.txt", "")]
        [InlineData("../../secret.txt", "")]
        [InlineData("secret.txt", "..")]
        public void Download_PathTraversal_ReturnsBadRequest(string file, string path)
        {
            var result = _controller.Download(file, path);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void Download_AbsolutePathAsFileName_ReturnsBadRequest()
        {
            var result = _controller.Download(_secretFile, "");

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void Delete_ValidFile_DeletesIt()
        {
            var target = Path.Combine(_testDirectory, "gone.txt");
            File.WriteAllText(target, "x");

            var result = _controller.Delete("gone.txt", "", isFolder: false);

            Assert.IsType<OkObjectResult>(result);
            Assert.False(File.Exists(target));
        }

        [Fact]
        public void Delete_FileOutsideHome_ReturnsBadRequestAndKeepsFile()
        {
            var result = _controller.Delete("../secret.txt", "", isFolder: false);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.True(File.Exists(_secretFile));
        }

        [Theory]
        [InlineData("..")]
        [InlineData(".")]
        [InlineData("../home-evil")]
        public void Delete_FolderOutsideOrEqualToHome_ReturnsBadRequestAndKeepsFolders(string name)
        {
            var result = _controller.Delete(name, "", isFolder: true);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.True(Directory.Exists(_testDirectory));
            Assert.True(Directory.Exists(_siblingDirectory));
            Assert.True(File.Exists(_secretFile));
        }

        [Fact]
        public void Move_ValidFile_MovesIt()
        {
            Directory.CreateDirectory(Path.Combine(_testDirectory, "dest"));
            File.WriteAllText(Path.Combine(_testDirectory, "a.txt"), "x");

            var result = _controller.Move("a.txt", "", "dest", isFolder: false);

            Assert.IsType<OkObjectResult>(result);
            Assert.True(File.Exists(Path.Combine(_testDirectory, "dest", "a.txt")));
        }

        [Fact]
        public void Move_SourceOutsideHome_ReturnsBadRequestAndKeepsFile()
        {
            var result = _controller.Move("../secret.txt", "", "", isFolder: false);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.True(File.Exists(_secretFile));
            Assert.False(File.Exists(Path.Combine(_testDirectory, "secret.txt")));
        }

        [Fact]
        public void Move_DestinationOutsideHome_ReturnsBadRequestAndKeepsFile()
        {
            var source = Path.Combine(_testDirectory, "a.txt");
            File.WriteAllText(source, "x");

            var result = _controller.Move("a.txt", "", "../home-evil", isFolder: false);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.True(File.Exists(source));
            Assert.False(File.Exists(Path.Combine(_siblingDirectory, "a.txt")));
        }

        [Fact]
        public void Copy_ValidFile_CopiesIt()
        {
            Directory.CreateDirectory(Path.Combine(_testDirectory, "dest"));
            File.WriteAllText(Path.Combine(_testDirectory, "a.txt"), "x");

            var result = _controller.Copy("a.txt", "", "dest", isFolder: false);

            Assert.IsType<OkObjectResult>(result);
            Assert.True(File.Exists(Path.Combine(_testDirectory, "a.txt")));
            Assert.True(File.Exists(Path.Combine(_testDirectory, "dest", "a.txt")));
        }

        [Fact]
        public void Copy_SourceOutsideHome_ReturnsBadRequest()
        {
            var result = _controller.Copy("../secret.txt", "", "", isFolder: false);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.False(File.Exists(Path.Combine(_testDirectory, "secret.txt")));
        }

        [Fact]
        public void Copy_DestinationIsSiblingSharingHomePrefix_ReturnsBadRequest()
        {
            File.WriteAllText(Path.Combine(_testDirectory, "a.txt"), "x");

            var result = _controller.Copy("a.txt", "", "../home-evil", isFolder: false);

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.False(File.Exists(Path.Combine(_siblingDirectory, "a.txt")));
        }

        [Fact]
        public void CreateFolder_Valid_CreatesIt()
        {
            var result = _controller.CreateFolder("new-folder", "");

            Assert.IsType<OkObjectResult>(result);
            Assert.True(Directory.Exists(Path.Combine(_testDirectory, "new-folder")));
        }

        [Theory]
        [InlineData("../escaped")]
        [InlineData("nested/child")]
        [InlineData("..")]
        public void CreateFolder_NameWithPathSegments_ReturnsBadRequest(string folderName)
        {
            var result = _controller.CreateFolder(folderName, "");

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.False(Directory.Exists(Path.Combine(_rootDirectory, "escaped")));
            Assert.False(Directory.Exists(Path.Combine(_testDirectory, "nested")));
        }

        [Fact]
        public async Task Upload_ValidFile_SavesIntoHome()
        {
            SetUploadedFile("notes.txt", "hello");

            var result = await _controller.Upload("");

            Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal("hello", File.ReadAllText(Path.Combine(_testDirectory, "notes.txt")));
        }

        [Theory]
        [InlineData("../uploaded.txt")]
        [InlineData("..\\uploaded.txt")]
        [InlineData("../../uploaded.txt")]
        public async Task Upload_FileNameWithPathSegments_SavesOnlyTheNameInsideHome(string fileName)
        {
            SetUploadedFile(fileName, "payload");

            var result = await _controller.Upload("");

            Assert.IsType<OkObjectResult>(result.Result);
            Assert.True(File.Exists(Path.Combine(_testDirectory, "uploaded.txt")));
            Assert.False(File.Exists(Path.Combine(_rootDirectory, "uploaded.txt")));
            Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(_rootDirectory)!, "uploaded.txt")));
        }

        [Fact]
        public async Task Upload_FileNameDotDot_ReturnsBadRequest()
        {
            SetUploadedFile("..", "payload");

            var result = await _controller.Upload("");

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        private void SetUploadedFile(string fileName, string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var formFile = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "files", fileName);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Form = new FormCollection(
                new Dictionary<string, StringValues>(),
                new FormFileCollection { formFile });
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }

        public void Dispose()
        {
            if (Directory.Exists(_rootDirectory))
            {
                Directory.Delete(_rootDirectory, recursive: true);
            }

            GC.SuppressFinalize(this);
        }
    }
}
