using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Project
    {
        [Key]
        public int ProjectID { get; set; }
        public string ProjectName { get; set; }
        public ICollection<Sector>? Sectors { get; set; } = new List<Sector>();
        public ICollection<Donor>? Donors { get; set; } = new List<Donor>();
        public ICollection<Ministry> Ministries { get; set; } = new List<Ministry>();
        public double EstimatedBudget { get; set; }
        public double RealBudget { get; set; }
        public int ProjectManagerCode { get; set; }
        public virtual ProjectManager ProjectManager { get; set; }
        public int SuperVisorCode { get; set; }
        public virtual SuperVisor SuperVisor { get; set; }
        public int? GoalCode { get; set; }
        public virtual Goal Goal { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public double performance { get; set; }
        public double DisbursementPerformance { get; set; }
        public double FieldMonitoring { get; set; }
        public double ImpactAssessment { get; set; }
        public int Financial { get; set; }
        public int Physical { get; set; }

        // Navigation property for many-to-many relationship
        public ICollection<Measure> Measures { get; set; } = new List<Measure>();

        // New: Many-to-many with Indicator
        public ICollection<ProjectIndicator> ProjectIndicators { get; set; } = new List<ProjectIndicator>();

        // Navigation property for one-to-one relationship
        public ActionPlan ActionPlan { get; set; }
        public ICollection<LogicalFramework> logicalFramework { get; set; } = new List<LogicalFramework>();

        [NotMapped]
        public List<IFormFile> UploadedFiles { get; set; } = new List<IFormFile>();
        public ICollection<ProjectFile> ProjectFiles { get; set; } = new List<ProjectFile>();

        public ICollection<Governorate> Governorates { get; set; } = new List<Governorate>();
        public ICollection<District> Districts { get; set; } = new List<District>();
        public ICollection<SubDistrict> SubDistricts { get; set; } = new List<SubDistrict>();
        public ICollection<Community> Communities { get; set; } = new List<Community>();
        


        public void UpdatePerformance(double totalPlanned, double totalRealised)
        {
            if (totalPlanned > 0)
            {
                this.DisbursementPerformance = (int)((totalRealised / totalPlanned) * 100);
                this.FieldMonitoring = (int)((totalRealised / totalPlanned) * 100);
                this.ImpactAssessment = (int)((totalRealised / totalPlanned) * 100);
                this.Financial = (int)((totalRealised / totalPlanned) * 100);
                this.Physical = (int)((totalRealised / totalPlanned) * 100);
            }
            else
            {
                this.DisbursementPerformance = 0;
                this.FieldMonitoring = 0;
                this.ImpactAssessment = 0;
                this.Financial = 0;
                this.Physical = 0;
            }
        }
    }
}
