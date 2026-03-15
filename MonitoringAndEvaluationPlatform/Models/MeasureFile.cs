namespace MonitoringAndEvaluationPlatform.Models
{
    public class MeasureFile
    {
        public int Id { get; set; }

        public int MeasureCode { get; set; }
        public virtual Measure Measure { get; set; } = null!;

        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime UploadedDate { get; set; } = DateTime.Now;
    }
}
