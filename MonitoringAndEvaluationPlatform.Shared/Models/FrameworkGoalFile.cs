namespace MonitoringAndEvaluationPlatform.Models
{
    public class FrameworkGoalFile
    {
        public int Id { get; set; }

        public int FrameworkGoalID { get; set; }
        public FrameworkGoal FrameworkGoal { get; set; }

        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadedDate { get; set; } = DateTime.Now;
    }
}
