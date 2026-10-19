namespace MonitoringAndEvaluationPlatform.Services
{
    /// <summary>What a given upload endpoint is allowed to accept.</summary>
    public enum UploadPurpose
    {
        /// <summary>Images only (PNG/JPG/WebP), 5 MB.</summary>
        Image,

        /// <summary>Documents and images: PDF, Office, images, plain text/CSV. 25 MB.</summary>
        Attachment
    }

    /// <summary>
    /// Result of storing an upload. <paramref name="StoredFileName"/> is the server-generated
    /// name actually written to disk; <paramref name="RelativePath"/> is what belongs in the
    /// database. The caller-supplied filename is never used on disk.
    /// </summary>
    public record UploadResult(bool Ok, string? Error, string? StoredFileName, string? RelativePath);

    /// <summary>
    /// Central validation and storage for user uploads.
    ///
    /// Extracted from GuideService.SaveGuideImageAsync, which was the only upload path in the
    /// codebase that validated anything. The other five paths accepted any extension, any
    /// content type and any size, and one of them (FrameworkGoals) interpolated the raw
    /// client filename into the path, allowing directory traversal and arbitrary file
    /// overwrite.
    /// </summary>
    public interface IUploadValidationService
    {
        /// <summary>
        /// Validates <paramref name="file"/> against <paramref name="purpose"/> and, if it
        /// passes, writes it under <paramref name="relativeFolder"/> with a generated name.
        /// </summary>
        /// <param name="relativeFolder">
        /// Folder beneath the uploads root, e.g. "projects" or "frameworkgoals".
        /// </param>
        Task<UploadResult> SaveAsync(IFormFile file, UploadPurpose purpose, string relativeFolder);

        /// <summary>
        /// Validates without writing. Use when the caller needs to reject a batch before
        /// persisting any of it.
        /// </summary>
        (bool Ok, string? Error) Validate(IFormFile file, UploadPurpose purpose);

        /// <summary>
        /// Resolves a stored relative path to an absolute path, guaranteeing the result stays
        /// inside the uploads root. Returns null if it would escape.
        /// </summary>
        string? ResolveStoredPath(string relativePath);

        /// <summary>
        /// Strips a caller-supplied filename down to something safe to store and display.
        /// Never used to build a disk path - only for the display name held in the database.
        /// </summary>
        string SanitizeDisplayName(string fileName);
    }
}
