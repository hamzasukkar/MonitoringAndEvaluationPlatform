using MonitoringAndEvaluationPlatform.Services;

namespace MonitoringAndEvaluationPlatform.Helpers
{
    /// <summary>
    /// View and controller helpers on top of <see cref="IUploadValidationService"/>, which owns
    /// the actual rules: allowed types, size cap, content check, and storage outside wwwroot.
    /// </summary>
    public static class UploadFileRules
    {
        /// <summary>Value for an &lt;input type="file" accept="..."&gt; attribute.</summary>
        public static string AcceptAttribute => string.Join(",", UploadValidationService.AllowedAttachmentExtensions);

        /// <summary>
        /// Display names of the non-empty files the upload service would refuse (type or size),
        /// so a batch can be rejected with one localized message before anything is saved.
        /// </summary>
        public static List<string> Rejected(IUploadValidationService uploads, IEnumerable<IFormFile>? files) =>
            files?.Where(f => f.Length > 0 && !uploads.Validate(f, UploadPurpose.Attachment).Ok)
                  .Select(f => uploads.SanitizeDisplayName(f.FileName))
                  .ToList() ?? new List<string>();
    }
}
