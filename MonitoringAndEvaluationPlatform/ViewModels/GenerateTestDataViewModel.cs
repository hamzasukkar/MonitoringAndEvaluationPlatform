using System.ComponentModel.DataAnnotations;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.ViewModels
{
    /// <summary>
    /// Configuration for one run of the admin test-data generator. Every count is capped so a
    /// mistyped value cannot explode into a multi-hundred-thousand row insert; the service
    /// re-checks <see cref="EstimatedRowCount"/> against <see cref="MaxRowCount"/> server-side.
    /// </summary>
    public class GenerateTestDataViewModel
    {
        /// <summary>Hard ceiling on a single run, enforced in the service (never trust the client estimate).</summary>
        public const int MaxRowCount = 50_000;

        public const string DeleteConfirmationPhrase = "DELETE TEST DATA";

        [Required(ErrorMessage = "Please choose a ministry.")]
        [Display(Name = "Ministry")]
        public int MinistryCode { get; set; }

        /// <summary>
        /// Tags every generated entity's name. This is the only handle the delete action has,
        /// so it must be non-empty and is matched with StartsWith.
        /// </summary>
        [Required(ErrorMessage = "A name prefix is required so generated data can be deleted later.")]
        [StringLength(20, MinimumLength = 1, ErrorMessage = "Prefix must be between 1 and 20 characters.")]
        [Display(Name = "Name Prefix")]
        public string NamePrefix { get; set; } = "[TEST] ";

        /// <summary>Blank falls back to "{prefix}Framework {timestamp}".</summary>
        [StringLength(150)]
        [Display(Name = "Framework Name")]
        public string? FrameworkName { get; set; }

        [Range(0, 10, ErrorMessage = "Outcomes must be between 0 and 10.")]
        [Display(Name = "Outcomes per Framework")]
        public int OutcomesPerFramework { get; set; } = 2;

        [Range(0, 10, ErrorMessage = "Outputs must be between 0 and 10.")]
        [Display(Name = "Outputs per Outcome")]
        public int OutputsPerOutcome { get; set; } = 2;

        [Range(0, 10, ErrorMessage = "Sub-outputs must be between 0 and 10.")]
        [Display(Name = "Sub-Outputs per Output")]
        public int SubOutputsPerOutput { get; set; } = 2;

        [Range(0, 10, ErrorMessage = "Indicators must be between 0 and 10.")]
        [Display(Name = "Indicators per Sub-Output")]
        public int IndicatorsPerSubOutput { get; set; } = 2;

        /// <summary>
        /// Indicator.ProjectID is a single nullable FK, so an indicator links to at most one
        /// project — this is inherently a yes/no, not a count.
        /// </summary>
        [Display(Name = "Create a project for each indicator")]
        public bool CreateProjectPerIndicator { get; set; } = true;

        [Range(1, 9, ErrorMessage = "Phases must be between 1 and 9.")]
        [Display(Name = "Phases per Project")]
        public int PhasesPerProject { get; set; } = 1;

        [Range(1, 60, ErrorMessage = "Duration must be between 1 and 60 months.")]
        [Display(Name = "Project Duration (months)")]
        public int ProjectDurationMonths { get; set; } = 12;

        /// <summary>
        /// When false the tree is created with every value at zero. When true, measures and
        /// disbursement amounts are randomised and the performance cascade is run so the
        /// dashboards show non-zero figures.
        /// </summary>
        [Display(Name = "Populate performance values")]
        public bool PopulateValues { get; set; } = true;

        [Range(0, 12, ErrorMessage = "Measures must be between 0 and 12.")]
        [Display(Name = "Measures per Phase")]
        public int MeasuresPerPhase { get; set; } = 3;

        /// <summary>Populated by the controller for the dropdown; not posted back.</summary>
        public List<Ministry> AvailableMinistries { get; set; } = new List<Ministry>();

        public int OutcomeCount => OutcomesPerFramework;
        public int OutputCount => OutcomeCount * OutputsPerOutcome;
        public int SubOutputCount => OutputCount * SubOutputsPerOutput;
        public int IndicatorCount => SubOutputCount * IndicatorsPerSubOutput;
        public int ProjectCount => CreateProjectPerIndicator ? IndicatorCount : 0;
        public int PhaseCount => ProjectCount * PhasesPerProject;
        public int ActionPlanCount => PhaseCount;

        /// <summary>Inclusive of both the start and end month, matching CreateDefaultPlansForActionPlanAsync.</summary>
        public int PlanCount => PhaseCount * (ProjectDurationMonths + 1);

        public int MeasureCount => PopulateValues ? PhaseCount * MeasuresPerPhase : 0;

        public int EstimatedRowCount =>
            1 + OutcomeCount + OutputCount + SubOutputCount + IndicatorCount
            + ProjectCount + PhaseCount + ActionPlanCount + PlanCount + MeasureCount;
    }

    /// <summary>Per-entity counts from a completed generation or deletion, used to build the flash message.</summary>
    public class TestDataGenerationResult
    {
        public int Frameworks { get; set; }
        public int Outcomes { get; set; }
        public int Outputs { get; set; }
        public int SubOutputs { get; set; }
        public int Indicators { get; set; }
        public int Projects { get; set; }
        public int Phases { get; set; }
        public int ActionPlans { get; set; }
        public int Plans { get; set; }
        public int Measures { get; set; }

        public int Total =>
            Frameworks + Outcomes + Outputs + SubOutputs + Indicators
            + Projects + Phases + ActionPlans + Plans + Measures;

        public override string ToString() =>
            $"{Frameworks} framework(s), {Outcomes} outcome(s), {Outputs} output(s), " +
            $"{SubOutputs} sub-output(s), {Indicators} indicator(s), {Projects} project(s), " +
            $"{Phases} phase(s), {ActionPlans} action plan(s), {Plans} plan(s), {Measures} measure(s)";
    }
}
