using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MonitoringAndEvaluationPlatform.Attributes;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Project
    {
        [Key]
        public int ProjectID { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources.Models.Project), ErrorMessageResourceName = "ProjectNameRequired")]
        [StringLength(200, MinimumLength = 3, ErrorMessageResourceType = typeof(Resources.Models.Project), ErrorMessageResourceName = "ProjectNameLength")]
        [Display(Name = "Project Name")]
        public string ProjectName { get; set; } = string.Empty;

        public ICollection<Sector>? Sectors { get; set; } = new List<Sector>();
        public ICollection<Donor>? Donors { get; set; } = new List<Donor>();
        public ICollection<ProjectDonor> ProjectDonors { get; set; } = new List<ProjectDonor>();
        public ICollection<Ministry> Ministries { get; set; } = new List<Ministry>();

        [Display(Name = "Ministry")]
        public int? MinistryCode { get; set; }
        public virtual Ministry? Ministry { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources.Models.Project), ErrorMessageResourceName = "EstimatedBudgetRequired")]
        [Range(0.01, double.MaxValue, ErrorMessageResourceType = typeof(Resources.Models.Project), ErrorMessageResourceName = "EstimatedBudgetRange")]
        [Display(Name = "Estimated Budget ($)")]
        public double EstimatedBudget { get; set; }

        [Range(0, double.MaxValue, ErrorMessageResourceType = typeof(Resources.Models.Project), ErrorMessageResourceName = "RealBudgetRange")]
        [Display(Name = "Real Budget ($)")]
        public double RealBudget { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources.Models.Project), ErrorMessageResourceName = "ProjectManagerRequired")]
        [Display(Name = "Project Manager")]
        public int ProjectManagerCode { get; set; }
        public virtual ProjectManager? ProjectManager { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources.Models.Project), ErrorMessageResourceName = "SupervisorRequired")]
        [Display(Name = "Supervisor")]
        public int SuperVisorCode { get; set; }
        public virtual SuperVisor? SuperVisor { get; set; }

        [Display(Name = "SDG Goal")]
        public int? GoalCode { get; set; }
        public virtual Goal? Goal { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources.Models.Project), ErrorMessageResourceName = "StartDateRequired")]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources.Models.Project), ErrorMessageResourceName = "EndDateRequired")]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        [DateRangeValidation(nameof(StartDate))]
        public DateTime EndDate { get; set; }
        public double performance { get; set; }
        public double DisbursementPerformance { get; set; }
        public double FieldMonitoring { get; set; }
        public double ImpactAssessment { get; set; }
        public int Financial { get; set; }
        public int Physical { get; set; }

        // Many-to-many with Indicator
        public ICollection<ProjectIndicator> ProjectIndicators { get; set; } = new List<ProjectIndicator>();

        // Navigation property for one-to-one relationship
        public ActionPlan ActionPlan { get; set; }

        [NotMapped]
        public List<IFormFile> UploadedFiles { get; set; } = new List<IFormFile>();
        public ICollection<ProjectFile> ProjectFiles { get; set; } = new List<ProjectFile>();

        public ICollection<Governorate> Governorates { get; set; } = new List<Governorate>();
        public ICollection<District> Districts { get; set; } = new List<District>();
        public ICollection<SubDistrict> SubDistricts { get; set; } = new List<SubDistrict>();
        public ICollection<Community> Communities { get; set; } = new List<Community>();

        [Display(Name = "Entire Country")]
        public bool IsEntireCountry { get; set; } = false;


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
