using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Program
    {
        [Key]
        public int ProjectID { get; set; }
        public string ProjectName { get; set; }
        public int RegionCode { get; set; }
        public virtual Region Region { get; set; }
        public double EstimatedBudget { get; set; }
        public double RealBudget { get; set; }
        public string Trend { get; set; }
        public string ProjectManager { get; set; }
        public string SuperVisor { get; set; }
        public string Type { get; set; }
        public string Status1 { get; set; }
        public string Status2 { get; set; }
        public string Category { get; set; }
        public string Donor { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int performance { get; set; }
        public int DisbursementPerformance { get; set; }
        public int FieldMonitoring { get; set; }
        public int ImpactAssessment { get; set; }
    }
}
