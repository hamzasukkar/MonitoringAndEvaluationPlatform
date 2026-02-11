namespace MonitoringAndEvaluationPlatform.Models
{
    public class ProjectFile
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }
        public Project Project { get; set; }

        public string FileName { get; set; }
        public string FilePath { get; set; }
    }
}
