using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Services
{
    /// <summary>
    /// An Indicator and the Project created from it through "Add &amp; Create Project" live as a pair:
    /// created together, deleted together (see <c>MonitoringService.DeleteProjectAndRecalculateAsync</c>),
    /// and sharing a name. This service owns the creation and renaming halves of that contract so both
    /// IndicatorsController and ProjectsController apply the same rules.
    /// </summary>
    public class IndicatorProjectPairService
    {
        private readonly ApplicationDbContext _context;

        public IndicatorProjectPairService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// True when the suboutput already holds an indicator with this name. Checked both before the
        /// project form opens and again before the pair is inserted, since the two are separate requests.
        /// </summary>
        public async Task<bool> IndicatorNameExistsInSubOutputAsync(int subOutputCode, string name)
        {
            var normalized = name.Trim().ToLower();

            return await _context.Indicators
                .AnyAsync(i => i.SubOutputCode == subOutputCode && i.Name.ToLower() == normalized);
        }

        /// <summary>
        /// Spreads weight equally across every indicator of a suboutput so the total stays exactly 100.
        /// </summary>
        public async Task RedistributeIndicatorWeightsAsync(int subOutputCode)
        {
            var indicators = await _context.Indicators
                .Where(i => i.SubOutputCode == subOutputCode)
                .ToListAsync();

            if (indicators.Count == 0)
                return;

            double equalWeight = Math.Round(100.0 / indicators.Count, 2);
            foreach (var indicator in indicators)
            {
                indicator.Weight = equalWeight;
            }

            // Absorb the rounding remainder into the last one so the total is exactly 100.
            double total = indicators.Sum(i => i.Weight);
            if (Math.Abs(total - 100.0) > 0.01)
            {
                indicators[^1].Weight = Math.Round(indicators[^1].Weight + (100.0 - total), 2);
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Renames the indicators of <paramref name="project"/> that still carried the project's previous
        /// name. Indicators the user deliberately named something else are left untouched — that also keeps
        /// a project linked to several indicators (via LinkProjectToIndicators) from collapsing them all
        /// onto one name. Requires <c>project.Indicators</c> to be loaded; the caller owns SaveChanges.
        /// </summary>
        public int SyncIndicatorNamesToProject(Project project, string? previousName, string? newName)
        {
            if (!HasRenamed(previousName, newName) || project.Indicators == null)
                return 0;

            int renamed = 0;
            foreach (var indicator in project.Indicators)
            {
                if (NamesMatch(indicator.Name, previousName))
                {
                    indicator.Name = newName!;
                    renamed++;
                }
            }

            return renamed;
        }

        /// <summary>
        /// The mirror of <see cref="SyncIndicatorNamesToProject"/>: renames the linked project when it still
        /// carried the indicator's previous name. The caller owns SaveChanges.
        /// </summary>
        public async Task<bool> SyncProjectNameToIndicatorAsync(Indicator indicator, string? previousName, string? newName)
        {
            if (!HasRenamed(previousName, newName) || !indicator.ProjectID.HasValue)
                return false;

            var project = await _context.Projects.FindAsync(indicator.ProjectID.Value);
            if (project == null || !NamesMatch(project.ProjectName, previousName))
                return false;

            project.ProjectName = newName!;
            return true;
        }

        // Ordinal, so that a change of casing alone still counts as a rename worth propagating.
        private static bool HasRenamed(string? previousName, string? newName) =>
            !string.IsNullOrWhiteSpace(newName) && !string.Equals(previousName, newName, StringComparison.Ordinal);

        // Case- and whitespace-insensitive, so a pair counts as "still in sync" despite sloppy input.
        private static bool NamesMatch(string? left, string? right) =>
            string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
