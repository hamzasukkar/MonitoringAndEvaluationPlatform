using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringAndEvaluationPlatform.Models
{
    /// <summary>
    /// A unit of measurement defined once for the whole system — "km", "school", "beneficiary".
    ///
    /// The type is called MeasurementUnit rather than Unit so that every entity measured in one
    /// can keep a property literally named <c>Unit</c>. Everywhere the user sees it, it is
    /// "Units": the DbSet, the table and the /Units route all use that name.
    ///
    /// Before this existed the same unit was retyped as free text on every form, so "km", "Km"
    /// and "كم" were three different units and none of them could be translated.
    /// </summary>
    public class MeasurementUnit
    {
        [Key]
        public int Code { get; set; }

        [Required(ErrorMessage = "English name is required.")]
        [StringLength(100)]
        [Display(Name = "English Name")]
        public string EN_Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Arabic name is required.")]
        [StringLength(100)]
        [Display(Name = "Arabic Name")]
        public string AR_Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional: French is a supported culture but is not offered in the language switcher,
        /// so most units will never have one. <see cref="DisplayName"/> falls back to English.
        /// </summary>
        [StringLength(100)]
        [Display(Name = "French Name")]
        public string? FR_Name { get; set; }

        public ICollection<ImpactIndicator> ImpactIndicators { get; set; } = new List<ImpactIndicator>();
        public ICollection<FrameworkGoal> FrameworkGoals { get; set; } = new List<FrameworkGoal>();
        public ICollection<Measure> Measures { get; set; } = new List<Measure>();

        /// <summary>
        /// The name for the current UI culture, falling back to English and then Arabic so a
        /// half-filled row still renders something rather than an empty string.
        /// </summary>
        [NotMapped]
        public string DisplayName
        {
            get
            {
                var culture = System.Globalization.CultureInfo.CurrentUICulture.Name;

                var preferred = culture.StartsWith("ar") ? AR_Name
                              : culture.StartsWith("fr") ? FR_Name
                              : EN_Name;

                return FirstNonBlank(preferred, EN_Name, AR_Name) ?? string.Empty;
            }
        }

        private static string? FirstNonBlank(params string?[] candidates) =>
            candidates.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));
    }
}
