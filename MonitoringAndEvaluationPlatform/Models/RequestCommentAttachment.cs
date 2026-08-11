namespace MonitoringAndEvaluationPlatform.Models
{
    public class RequestCommentAttachment
    {
        public int Id { get; set; }

        public int RequestCommentId { get; set; }
        public RequestComment RequestComment { get; set; }

        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadedDate { get; set; } = DateTime.Now;
    }
}
