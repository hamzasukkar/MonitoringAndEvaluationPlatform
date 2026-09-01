using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MonitoringAndEvaluationPlatform.ViewModel
{
    /// <summary>
    /// Backs the "Add Project Output" page. The name and all three link selections post together
    /// in a single form, and the links are attached on submit.
    ///
    /// The Available* lists are display-only and MUST be repopulated before redisplaying the view
    /// after a validation failure — otherwise the pickers render empty (the bug that
    /// ProjectsController.LinkProjectToIndicators has).
    /// </summary>
    public class ProjectOutputFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Project output name is required.")]
        [StringLength(300, MinimumLength = 2, ErrorMessage = "Project output name must be between 2 and 300 characters.")]
        [Display(Name = "Development Impact Indicator Name")]
        public string Name { get; set; } = string.Empty;

        // Bounds for the Amount of Change column on the Impact table. Optional, like every other
        // field on this form except Name — an output can be created before its bounds are known.
        // A Target BELOW Base is allowed on purpose: a decrease goal is legitimate, and
        // FrameworkGoal already supports one (IsIncreaseGoal / AnnualChangeRate handle it).
        [Display(Name = "Base Value")]
        public double? BaseValue { get; set; }

        [Display(Name = "Target")]
        public double? TargetValue { get; set; }

        // Nullable on purpose. The project has <Nullable>enable</Nullable>, so a non-nullable
        // List<int> picks up an IMPLICIT [Required] and the tag helper emits
        // data-val-required — which would make unobtrusive validation refuse to submit the form
        // unless all three pickers had a selection. Leaving any of them empty is legitimate.
        [Display(Name = "Ministries")]
        public List<int>? SelectedMinistryCodes { get; set; } = new();

        [Display(Name = "Frameworks")]
        public List<int>? SelectedFrameworkCodes { get; set; } = new();

        [Display(Name = "Impact Indicators")]
        public List<int>? SelectedImpactIndicatorIds { get; set; } = new();

        // Parallel to SelectedImpactIndicatorIds: the weight (0-100) assigned to each selected
        // indicator, keyed by its id. A JS-rendered row per currently-selected indicator on the
        // Create form keeps this in sync with the multi-select above it and defaults new rows to
        // an equal share of 100. The controller additionally validates that every linked
        // indicator's weight sums to exactly 100 — a cross-row rule this per-item Range attribute
        // cannot express on its own.
        public List<IndicatorWeightInput>? IndicatorWeights { get; set; } = new();

        public List<SelectListItem> AvailableMinistries { get; set; } = new();
        public List<SelectListItem> AvailableFrameworks { get; set; } = new();
        public List<SelectListItem> AvailableImpactIndicators { get; set; } = new();
    }

    public class IndicatorWeightInput
    {
        public int ImpactIndicatorId { get; set; }

        [Range(0, 100, ErrorMessage = "Weight must be between 0 and 100.")]
        public double? Weight { get; set; }
    }
}
