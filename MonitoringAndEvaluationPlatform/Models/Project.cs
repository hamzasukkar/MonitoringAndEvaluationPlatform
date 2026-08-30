using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MonitoringAndEvaluationPlatform.Attributes;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class Project : IHasTimestamps
    {
        [Key]
        public int ProjectID { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources.Models.Project), ErrorMessageResourceName = "ProjectNameRequired")]
        [StringLength(200, MinimumLength = 3, ErrorMessageResourceType = typeof(Resources.Models.Project), ErrorMessageResourceName = "ProjectNameLength")]
        [Display(Name = "Project Name")]
        public string ProjectName { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(Resources.Models.Project), ErrorMessageResourceName = "SectorRequired")]
        [Display(Name = "Sector")]
        public int SectorCode { get; set; }
        public virtual Sector? Sector { get; set; }

        [Display(Name = "Public Sector Type")]
        public int? PublicSectorTypeCode { get; set; }
        public virtual PublicSectorType? PublicSectorType { get; set; }

        public ICollection<Donor>? Donors { get; set; } = new List<Donor>();
        public ICollection<ProjectDonor> ProjectDonors { get; set; } = new List<ProjectDonor>();
        public ICollection<Ministry> Ministries { get; set; } = new List<Ministry>();

        [Display(Name = "Ministry")]
        public int? MinistryCode { get; set; }
        public virtual Ministry? Ministry { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources.Models.Project), ErrorMessageResourceName = "EstimatedBudgetRequired")]
        [Range(0.01, double.MaxValue, ErrorMessageResourceType = typeof(Resources.Models.Project), ErrorMessageResourceName = "EstimatedBudgetRange")]
        [Display(Name = "Estimated Budget")]
        public double EstimatedBudget { get; set; }

        [Display(Name = "Currency")]
        public string Currency { get; set; } = "USD";

        [Display(Name = "Budget Unit")]
        public BudgetUnit BudgetUnit { get; set; } = BudgetUnit.Ones;

        /// <summary>
        /// How many Syrian Pounds one unit of <see cref="Currency"/> is worth, captured when
        /// this project was entered. Required for every non-SYP currency; stays null for SYP
        /// projects, which convert at 1:1 and need no rate.
        /// </summary>
        /// <remarks>
        /// This is the project's own historical snapshot and always wins over the platform-wide
        /// <see cref="CurrencyRate"/> fallback, which only covers legacy rows saved before the
        /// rate became mandatory. Note the meaning changed: it previously held "SYP per USD" on
        /// SYP projects to drive a USD-equivalent display; those values were cleared by migration
        /// because they are meaningless under this definition.
        /// </remarks>
        [Display(Name = "Exchange Rate to SYP")]
        [Range(0.0001, double.MaxValue)]
        [Column(TypeName = "decimal(18,4)")]
        [RequiredWhenCurrencyNotSyp(nameof(Currency),
            ErrorMessageResourceType = typeof(Resources.Models.Project),
            ErrorMessageResourceName = "ExchangeRateRequired")]
        public decimal? ExchangeRate { get; set; }

        [Display(Name = "Exchange Rate Date")]
        [DataType(DataType.Date)]
        public DateTime? ExchangeRateDate { get; set; }

        [NotMapped]
        public string CurrencySymbol => Currency switch
        {
            "USD" => "$",
            "EUR" => "\u20AC",
            "SYP" => "SYP ",
            _ => "$"
        };

        [Range(0, double.MaxValue, ErrorMessageResourceType = typeof(Resources.Models.Project), ErrorMessageResourceName = "RealBudgetRange")]
        [Display(Name = "Real Budget")]
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

        // Project Phases — the new intermediate layer
        public virtual ICollection<ProjectPhase> Phases { get; set; } = new List<ProjectPhase>();

        // Indicators linked directly to this project (one-to-many, replacing the old many-to-many via ProjectIndicator)
        public virtual ICollection<Indicator> Indicators { get; set; } = new List<Indicator>();

        // Impact indicators — a parallel measurement track that does not feed `performance`
        public virtual ICollection<ImpactIndicator> ImpactIndicators { get; set; } = new List<ImpactIndicator>();

        /// <summary>
        /// The years this project spans, derived from its own date range. This is the year set
        /// impact indicators are measured over — years are never stored on the indicator itself,
        /// so moving the project's dates moves the grid with it.
        ///
        /// StartDate/EndDate are both required and already guarded by [DateRangeValidation], so
        /// the range is always valid; Math.Max is belt-and-braces for a same-year project.
        /// </summary>
        [NotMapped]
        public IEnumerable<int> CoveredYears =>
            Enumerable.Range(StartDate.Year, Math.Max(EndDate.Year - StartDate.Year + 1, 1));

        [NotMapped]
        public List<IFormFile> UploadedFiles { get; set; } = new List<IFormFile>();
        public ICollection<ProjectFile> ProjectFiles { get; set; } = new List<ProjectFile>();

        public ICollection<Governorate> Governorates { get; set; } = new List<Governorate>();
        public ICollection<District> Districts { get; set; } = new List<District>();
        public ICollection<SubDistrict> SubDistricts { get; set; } = new List<SubDistrict>();
        public ICollection<Community> Communities { get; set; } = new List<Community>();

        [Display(Name = "Entire Country")]
        public bool IsEntireCountry { get; set; } = false;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Last Modified")]
        public DateTime LastModifiedAt { get; set; }
    }
}
