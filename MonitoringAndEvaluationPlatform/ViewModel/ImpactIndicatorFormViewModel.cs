using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    /// <summary>
    /// Backs both Create and Edit for an impact indicator. The indicator's own fields and the
    /// whole per-year grid are posted together in a single form, so one save covers both.
    ///
    /// The year set is derived from the parent project's date range and re-derived on every
    /// request — the posted Year values are only a hint, and the controller validates each one
    /// against the project before writing.
    ///
    /// Numeric fields are strings, deliberately. Model binding a double would use the request
    /// culture and reject "12,5" from Arabic- and French-locale users; the controller parses
    /// them with the same comma/period normalisation FrameworkGoalsController uses.
    /// </summary>
    public class ImpactIndicatorFormViewModel
    {
        public int Id { get; set; }

        public int ProjectID { get; set; }

        /// <summary>Display only; never trusted on post.</summary>
        public string ProjectName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Indicator name is required.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Indicator name must be between 2 and 200 characters.")]
        [Display(Name = "Indicator Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Unit")]
        public string? Unit { get; set; }

        /// <summary>
        /// Parsed and range-checked in the controller (must be greater than zero).
        /// </summary>
        [Required(ErrorMessage = "Target value is required.")]
        [Display(Name = "Target Value")]
        public string? TargetValue { get; set; }

        public List<YearValueInput> YearValues { get; set; } = new();
    }

    public class YearValueInput
    {
        public int Year { get; set; }

        /// <summary>
        /// Null or blank means "not entered" and writes no row — deliberately distinct from a
        /// recorded 0. Clearing a previously-entered year deletes its row.
        /// </summary>
        [Display(Name = "Value")]
        public string? Value { get; set; }
    }
}
