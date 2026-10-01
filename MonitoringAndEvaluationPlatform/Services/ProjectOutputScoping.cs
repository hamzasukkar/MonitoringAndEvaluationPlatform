using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Services
{
    /// <summary>
    /// A ProjectOutput can be shared by several ministries. A ministry user sees such an output,
    /// but only their own part of it: their own ministry tag, their own strategies, and the
    /// indicators measured by their own projects. Every rolled-up figure on ProjectOutput reads
    /// IndicatorLinks, so once the links are trimmed the percentages are computed from the
    /// user's own indicators, with the weights re-normalised over what remains.
    /// </summary>
    public static class ProjectOutputScoping
    {
        /// <summary>
        /// Trims <paramref name="output"/> in place. The entity MUST be untracked (loaded with
        /// AsNoTracking): on a tracked entity the removed links would be deleted by the next
        /// SaveChanges. Administrators see the whole output, so their copy is left untouched.
        /// Needs IndicatorLinks → ImpactIndicator → Project loaded.
        /// </summary>
        public static void TrimToScope(this ProjectOutput output, MinistryScope scope)
        {
            if (scope.IsAdmin) return;

            output.Ministries = output.Ministries.Where(m => scope.CanSee(m.Code)).ToList();
            output.Frameworks = output.Frameworks.Where(f => scope.CanSee(f.MinistryCode)).ToList();
            output.IndicatorLinks = output.IndicatorLinks
                .Where(l => l.ImpactIndicator?.Project != null && scope.CanSee(l.ImpactIndicator.Project.MinistryCode))
                .ToList();
        }

        /// <summary>
        /// A legacy row can link an indicator under one ministry's strategy to a project owned by
        /// another. The indicator is the viewer's; the project is not, so it is detached before
        /// rendering. The indicators MUST be untracked (AsNoTracking): on a tracked entity,
        /// clearing Project would null its ProjectID on the next SaveChanges.
        /// </summary>
        public static void HideForeignProjects(this IEnumerable<Indicator> indicators, MinistryScope scope)
        {
            if (scope.IsAdmin) return;

            foreach (var indicator in indicators)
            {
                if (indicator.Project != null && !scope.CanSee(indicator.Project.MinistryCode))
                {
                    indicator.Project = null;
                }
            }
        }
    }
}
