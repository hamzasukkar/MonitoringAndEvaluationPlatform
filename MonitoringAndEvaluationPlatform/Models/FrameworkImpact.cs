using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringAndEvaluationPlatform.Models
{
    /// <summary>
    /// A strategic impact target owned by a Framework — "raise health coverage from 20% to 80%
    /// between 2020 and 2030" — whose progress is driven by a weighted set of project-level
    /// <see cref="ImpactIndicator"/>s.
    ///
    /// Deliberately separate from <see cref="FrameworkGoal"/>, which carries the same five
    /// baseline/target fields but whose current value is TYPED IN by a user each year
    /// (BaseValueForCurrentYear). Here the current achievement is COMPUTED from linked indicators,
    /// which is a different enough contract to warrant its own entity rather than bolting
    /// indicator links onto FrameworkGoal's 1228-line controller and 6080-line view.
    /// </summary>
    public class FrameworkImpact
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters.")]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Baseline year is required.")]
        [Range(1900, 2200, ErrorMessage = "Baseline year must be a valid year.")]
        [Display(Name = "Baseline Year")]
        public int BaselineYear { get; set; }

        [Required(ErrorMessage = "Baseline value is required.")]
        [Display(Name = "Baseline Value")]
        public double BaselineValue { get; set; }

        [Required(ErrorMessage = "Target year is required.")]
        [Range(1900, 2200, ErrorMessage = "Target year must be a valid year.")]
        [Display(Name = "Target Year")]
        public int TargetYear { get; set; }

        [Required(ErrorMessage = "Target value is required.")]
        [Display(Name = "Target Value")]
        public double TargetValue { get; set; }

        /// <summary>
        /// Unit the baseline and target are expressed in (%, km, beneficiary). Without it
        /// "from 20 to 80" is unreadable in reports — the same lesson as ImpactIndicator.Unit.
        /// </summary>
        [StringLength(100)]
        [Display(Name = "Unit")]
        public string? Unit { get; set; }

        [StringLength(1000)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        public int FrameworkCode { get; set; }
        public virtual Framework Framework { get; set; } = null!;

        public virtual ICollection<FrameworkImpactIndicator> Indicators { get; set; }
            = new List<FrameworkImpactIndicator>();

        /// <summary>
        /// Weighted average of the linked indicators' achievement rates.
        ///
        /// Only rates can be averaged here — the indicators have different units (schools, km,
        /// beneficiaries), so averaging their raw AchievedValue would produce a meaningless number.
        ///
        /// Computed, never stored, so it can never drift from its sources and picks up new
        /// achievements immediately. Requires the caller to have loaded
        /// <c>Indicators.ThenInclude(l =&gt; l.ImpactIndicator)</c>; with nothing loaded it reads 0.
        ///
        /// Dividing by the actual total weight rather than a hard-coded 100 keeps the result
        /// correct even while the weights are mid-edit and do not yet sum to 100.
        /// </summary>
        [NotMapped]
        [Display(Name = "Weighted Achievement Rate")]
        public double WeightedAchievementRate
        {
            get
            {
                var links = Indicators?.Where(l => l.ImpactIndicator != null).ToList();
                if (links is null || links.Count == 0) return 0;

                var totalWeight = links.Sum(l => l.Weight);
                if (totalWeight <= 0) return 0;

                return links.Sum(l => l.ImpactIndicator.AchievementRate * l.Weight) / totalWeight;
            }
        }

        /// <summary>Number of years the target spans; zero when the years are equal.</summary>
        [NotMapped]
        public int YearSpan => TargetYear - BaselineYear;

        /// <summary>True when the target is above the baseline (a growth target).</summary>
        [NotMapped]
        public bool IsIncreaseTarget => TargetValue > BaselineValue;
    }
}
