using System.Text.RegularExpressions;

namespace MonitoringAndEvaluationPlatform.Services
{
    /// <inheritdoc cref="IUploadValidationService"/>
    public class UploadValidationService : IUploadValidationService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UploadValidationService> _logger;

        private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".webp", ".gif" };

        private static readonly string[] AttachmentExtensions =
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
            ".png", ".jpg", ".jpeg", ".webp", ".gif", ".txt", ".csv"
        };

        private const long MaxImageBytes = 5 * 1024 * 1024;
        private const long MaxAttachmentBytes = 25 * 1024 * 1024;

        /// <summary>Characters kept in a stored display name. Everything else becomes '_'.</summary>
        private static readonly Regex UnsafeDisplayChars = new(@"[^\p{L}\p{N}\.\-_ ]", RegexOptions.Compiled);

        public UploadValidationService(
            IWebHostEnvironment env,
            IConfiguration configuration,
            ILogger<UploadValidationService> logger)
        {
            _env = env;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Root for user uploads. Defaults to App_Data/uploads under the content root, which is
        /// OUTSIDE wwwroot - uploads used to live in wwwroot and were therefore served by
        /// UseStaticFiles ahead of authentication, i.e. readable by anyone with the URL and
        /// executable as same-origin HTML/SVG.
        /// </summary>
        private string UploadsRoot
        {
            get
            {
                var configured = _configuration["Storage:UploadsRoot"];
                var root = string.IsNullOrWhiteSpace(configured)
                    ? Path.Combine(_env.ContentRootPath, "App_Data", "uploads")
                    : configured;

                Directory.CreateDirectory(root);
                return root;
            }
        }

        public (bool Ok, string? Error) Validate(IFormFile file, UploadPurpose purpose)
        {
            if (file == null || file.Length == 0)
            {
                return (false, "No file was selected.");
            }

            var (allowedExtensions, maxBytes) = LimitsFor(purpose);

            if (file.Length > maxBytes)
            {
                return (false, $"File exceeds the maximum size of {maxBytes / (1024 * 1024)} MB.");
            }

            // Only the base name is trusted, never a caller-supplied path.
            var extension = Path.GetExtension(Path.GetFileName(file.FileName) ?? string.Empty).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || Array.IndexOf(allowedExtensions, extension) < 0)
            {
                return (false, $"Unsupported file type. Allowed: {string.Join(", ", allowedExtensions)}.");
            }

            return (true, null);
        }

        public async Task<UploadResult> SaveAsync(IFormFile file, UploadPurpose purpose, string relativeFolder)
        {
            var (ok, error) = Validate(file, purpose);
            if (!ok)
            {
                return new UploadResult(false, error, null, null);
            }

            var extension = Path.GetExtension(Path.GetFileName(file.FileName)!).ToLowerInvariant();

            // The bytes must match the extension for formats we can recognise, so a renamed
            // .html or .svg cannot masquerade as an image.
            if (!await HasValidSignatureAsync(file, extension))
            {
                return new UploadResult(false, "The file contents do not match its extension.", null, null);
            }

            // The stored name is entirely server-generated. No part of the client filename
            // reaches the filesystem - this is what closes the traversal hole.
            var storedFileName = $"{Guid.NewGuid():N}{extension}";

            var folder = SanitizeFolder(relativeFolder);
            var directory = Path.Combine(UploadsRoot, folder);
            Directory.CreateDirectory(directory);

            var fullPath = Path.Combine(directory, storedFileName);

            // Defence in depth: the resolved path must stay inside the uploads root.
            if (!IsInsideUploadsRoot(fullPath))
            {
                return new UploadResult(false, "Invalid storage path.", null, null);
            }

            try
            {
                // FileMode.CreateNew, not Create: never silently overwrite an existing file.
                await using var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write);
                await file.CopyToAsync(stream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save upload to {Folder}", folder);
                return new UploadResult(false, "The file could not be saved.", null, null);
            }

            var relativePath = $"{folder}/{storedFileName}";
            return new UploadResult(true, null, storedFileName, relativePath);
        }

        public string? ResolveStoredPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return null;
            }

            // Tolerate legacy values that still carry a leading "/uploads/" prefix from when
            // files lived in wwwroot.
            var normalized = relativePath.Replace('\\', '/').TrimStart('/');
            var hadUploadsPrefix = normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase);
            if (hadUploadsPrefix)
            {
                normalized = normalized["uploads/".Length..];
            }

            var relative = normalized.Replace('/', Path.DirectorySeparatorChar);

            var fullPath = Path.Combine(UploadsRoot, relative);
            if (!IsInsideUploadsRoot(fullPath))
            {
                return null;
            }

            if (File.Exists(fullPath))
            {
                return fullPath;
            }

            // Fallback for rows written before uploads moved out of wwwroot. Those files are
            // no longer served statically (the pipeline 404s /uploads), so they remain
            // reachable only through the authorized download actions.
            var legacyRoot = Path.Combine(_env.WebRootPath, "uploads");
            var legacyPath = Path.Combine(legacyRoot, relative);
            var legacyRootFull = Path.GetFullPath(legacyRoot + Path.DirectorySeparatorChar);
            if (Path.GetFullPath(legacyPath).StartsWith(legacyRootFull, StringComparison.OrdinalIgnoreCase)
                && File.Exists(legacyPath))
            {
                return legacyPath;
            }

            return fullPath;
        }

        public string SanitizeDisplayName(string fileName)
        {
            var name = Path.GetFileName(fileName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(name))
            {
                return "file";
            }

            name = UnsafeDisplayChars.Replace(name, "_");

            // Keep display names bounded so they cannot break layouts or log lines.
            return name.Length > 128 ? name[^128..] : name;
        }

        private static (string[] Extensions, long MaxBytes) LimitsFor(UploadPurpose purpose) =>
            purpose switch
            {
                UploadPurpose.Image => (ImageExtensions, MaxImageBytes),
                _ => (AttachmentExtensions, MaxAttachmentBytes)
            };

        private static string SanitizeFolder(string relativeFolder)
        {
            var folder = (relativeFolder ?? string.Empty).Replace('\\', '/').Trim('/');

            // Reject any traversal segment outright rather than trying to clean it.
            if (folder.Length == 0 || folder.Split('/').Any(segment => segment is "." or ".."))
            {
                return "misc";
            }

            return folder;
        }

        private bool IsInsideUploadsRoot(string fullPath)
        {
            var rootFull = Path.GetFullPath(UploadsRoot + Path.DirectorySeparatorChar);
            return Path.GetFullPath(fullPath).StartsWith(rootFull, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks magic bytes for the formats we can identify. Extensions without a reliable
        /// signature (txt, csv, doc, xls, ppt) pass - the extension allow-list plus the fact
        /// that files are served with Content-Disposition: attachment is the control there.
        /// </summary>
        private static async Task<bool> HasValidSignatureAsync(IFormFile file, string extension)
        {
            var head = new byte[12];
            int read;
            await using (var stream = file.OpenReadStream())
            {
                read = await stream.ReadAsync(head.AsMemory(0, head.Length));
            }

            if (read < 4)
            {
                return false;
            }

            bool IsPng() => head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47;
            bool IsJpeg() => head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF;
            bool IsGif() => head[0] == 'G' && head[1] == 'I' && head[2] == 'F' && head[3] == '8';
            bool IsWebp() => read >= 12
                             && head[0] == 'R' && head[1] == 'I' && head[2] == 'F' && head[3] == 'F'
                             && head[8] == 'W' && head[9] == 'E' && head[10] == 'B' && head[11] == 'P';
            bool IsPdf() => head[0] == '%' && head[1] == 'P' && head[2] == 'D' && head[3] == 'F';
            // DOCX/XLSX/PPTX are ZIP containers.
            bool IsZip() => head[0] == 0x50 && head[1] == 0x4B
                            && (head[2] == 0x03 || head[2] == 0x05 || head[2] == 0x07);
            // Legacy OLE2 (.doc/.xls/.ppt).
            bool IsOle2() => head[0] == 0xD0 && head[1] == 0xCF && head[2] == 0x11 && head[3] == 0xE0;

            return extension switch
            {
                ".png" => IsPng(),
                ".jpg" or ".jpeg" => IsJpeg(),
                ".gif" => IsGif(),
                ".webp" => IsWebp(),
                ".pdf" => IsPdf(),
                ".docx" or ".xlsx" or ".pptx" => IsZip(),
                ".doc" or ".xls" or ".ppt" => IsOle2() || IsZip(),
                _ => true
            };
        }
    }
}
