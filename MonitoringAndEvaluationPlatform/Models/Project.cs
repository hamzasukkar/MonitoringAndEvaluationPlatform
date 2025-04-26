using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Project
    {
        [Key]
        public int ProjectID { get; set; }
        public string ProjectName { get; set; }
        public int RegionCode { get; set; }
        public virtual Region Region { get; set; }
        public double EstimatedBudget { get; set; }
        public double RealBudget { get; set; }
        public int ProjectManagerCode { get; set; }
        public virtual ProjectManager ProjectManager { get; set; }
        public int SuperVisorCode { get; set; }
        public virtual SuperVisor SuperVisor { get; set; }
        public int MinistryCode { get; set; }
        public Ministry Ministry { get; set; }
        public int DonorCode { get; set; }
        public virtual Donor Donor { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public double performance { get; set; }
        public int DisbursementPerformance { get; set; }
        public int FieldMonitoring { get; set; }
        public int ImpactAssessment { get; set; }

        // Navigation property for many-to-many relationship
        public ICollection<Measure> Measures { get; set; } = new List<Measure>();

        // New: Many-to-many with Indicator
        public ICollection<ProjectIndicator> ProjectIndicators { get; set; } = new List<ProjectIndicator>();

        // Navigation property for one-to-one relationship
        public ActionPlan ActionPlan { get; set; }

        public void UpdatePerformance(double totalPlanned, double totalRealised)
        {
            if (totalPlanned > 0)
            {
                this.DisbursementPerformance = (int)((totalRealised / totalPlanned) * 100);
                this.FieldMonitoring = (int)((totalRealised / totalPlanned) * 100);
                this.ImpactAssessment = (int)((totalRealised / totalPlanned) * 100);
            }
            else
            {
                this.DisbursementPerformance = 0;
                this.FieldMonitoring = 0;
                this.ImpactAssessment = 0;
            }
        }
    }
}
