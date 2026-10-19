using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MonitoringAndEvaluationPlatform.Services;

namespace MonitoringAndEvaluationPlatform.SecurityTests;

/// <summary>
/// Covers the upload controls. The original FrameworkGoals upload interpolated the raw
/// multipart filename into the destination path with FileMode.Create, so "../../web.config"
/// escaped the uploads folder and overwrote arbitrary files; none of the five upload paths
/// checked type, size or content.
/// </summary>
public class UploadValidationServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"mre-upload-tests-{Guid.NewGuid():N}");
    private readonly UploadValidationService _service;

    public UploadValidationServiceTests()
    {
        Directory.CreateDirectory(_root);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:UploadsRoot"] = Path.Combine(_root, "uploads")
            })
            .Build();

        _service = new UploadValidationService(
            new FakeWebHostEnvironment(_root),
            configuration,
            NullLogger<UploadValidationService>.Instance);
    }

    [Theory]
    [InlineData("../../../evil.png")]
    [InlineData("..\\..\\web.config")]
    [InlineData("/etc/passwd")]
    [InlineData("....//....//evil.png")]
    public async Task TraversalFilename_NeverEscapesUploadsRoot(string maliciousName)
    {
        var file = PngFile(maliciousName);

        var result = await _service.SaveAsync(file, UploadPurpose.Attachment, "frameworkgoals");

        // Either outcome is safe - rejected outright (e.g. ".config" is not on the allow-list,
        // "/etc/passwd" has no extension), or accepted under a generated name. What must never
        // happen is a write outside the uploads root, which is what the original code did.
        if (result.Ok)
        {
            // No part of the caller's name may survive into the stored name.
            Assert.DoesNotContain("..", result.StoredFileName!);
            Assert.DoesNotContain("/", result.StoredFileName!);
            Assert.DoesNotContain("\\", result.StoredFileName!);
            Assert.DoesNotContain("web.config", result.StoredFileName!);
            Assert.DoesNotContain("passwd", result.StoredFileName!);
        }

        var uploadsRoot = Path.GetFullPath(Path.Combine(_root, "uploads"));
        Directory.CreateDirectory(uploadsRoot);

        // Nothing may exist anywhere under the temp root except inside uploads/.
        foreach (var written in Directory.GetFiles(_root, "*", SearchOption.AllDirectories))
        {
            Assert.StartsWith(uploadsRoot, Path.GetFullPath(written), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task HtmlDisguisedAsPng_IsRejectedByContentCheck()
    {
        // The stored-XSS vector: an .html payload renamed .png, served same-origin.
        var file = FakeFile("payload.png", "<html><script>alert(document.cookie)</script></html>");

        var result = await _service.SaveAsync(file, UploadPurpose.Attachment, "projects");

        Assert.False(result.Ok);
        Assert.Contains("do not match", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("payload.html")]
    [InlineData("payload.svg")]
    [InlineData("payload.aspx")]
    [InlineData("payload.exe")]
    public async Task ExecutableOrMarkupExtensions_AreRejected(string fileName)
    {
        var file = FakeFile(fileName, "<script>alert(1)</script>");

        var result = await _service.SaveAsync(file, UploadPurpose.Attachment, "projects");

        Assert.False(result.Ok);
        Assert.Contains("Unsupported", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OversizeFile_IsRejected()
    {
        // 6 MB against the 5 MB image cap.
        var file = FakeFile("big.png", new string('a', 6 * 1024 * 1024));

        var result = await _service.SaveAsync(file, UploadPurpose.Image, "images");

        Assert.False(result.Ok);
        Assert.Contains("maximum size", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidPng_IsStoredWithGeneratedName()
    {
        var result = await _service.SaveAsync(PngFile("holiday photo.png"), UploadPurpose.Image, "images");

        Assert.True(result.Ok, result.Error);
        Assert.EndsWith(".png", result.StoredFileName);
        // The display name is not reused on disk.
        Assert.DoesNotContain("holiday", result.StoredFileName!);
        Assert.Equal($"images/{result.StoredFileName}", result.RelativePath);
    }

    [Theory]
    [InlineData("../../../../Windows/win.ini")]
    [InlineData("/../../appsettings.json")]
    public void ResolveStoredPath_RejectsEscapingPaths(string storedPath)
    {
        // Defence for a poisoned database value: resolution must not read outside the root.
        var resolved = _service.ResolveStoredPath(storedPath);

        if (resolved != null)
        {
            var uploadsRoot = Path.GetFullPath(Path.Combine(_root, "uploads")) + Path.DirectorySeparatorChar;
            Assert.StartsWith(uploadsRoot, Path.GetFullPath(resolved), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData("<img src=x onerror=alert(1)>.pdf")]
    [InlineData("../../evil.pdf")]
    public void SanitizeDisplayName_StripsMarkupAndPathSeparators(string raw)
    {
        var sanitized = _service.SanitizeDisplayName(raw);

        Assert.DoesNotContain("<", sanitized);
        Assert.DoesNotContain(">", sanitized);
        Assert.DoesNotContain("/", sanitized);
        Assert.DoesNotContain("\\", sanitized);
    }

    private static IFormFile PngFile(string fileName)
    {
        // Valid PNG magic bytes followed by filler.
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x00, 0x00 };
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName);
    }

    private static IFormFile FakeFile(string fileName, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public FakeWebHostEnvironment(string root)
        {
            ContentRootPath = root;
            WebRootPath = Path.Combine(root, "wwwroot");
            Directory.CreateDirectory(WebRootPath);
        }

        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "Tests";
        public string WebRootPath { get; set; }
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; }
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
