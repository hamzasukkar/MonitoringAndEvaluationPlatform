namespace MonitoringAndEvaluationPlatform.Models
{
    public class RequestFile
    {
        public int Id { get; set; }

        public int RequestId { get; set; }
        public Request Request { get; set; }

        public string FileName { get; set; }
        public string FilePath { get; set; }
    }
}
