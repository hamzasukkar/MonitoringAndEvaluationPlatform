namespace MonitoringAndEvaluationPlatform.ViewModels
{
    public class UserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? MinistryName { get; set; }
        public string? MinistryNameAr { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool LockoutEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public List<string> Roles { get; set; } = new List<string>();

        /// <summary>
        /// Whether the user completed authenticator enrolment. An administrator can clear it
        /// but can never set it - only the account holder can scan the QR code.
        /// </summary>
        public bool TwoFactorEnabled { get; set; }

        public bool IsLocked => LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;
    }
}
