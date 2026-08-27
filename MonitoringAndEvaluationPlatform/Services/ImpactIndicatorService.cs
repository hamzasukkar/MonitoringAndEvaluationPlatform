using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;

namespace MonitoringAndEvaluationPlatform.Services
{
    /// <summary>
    /// The single writer of <c>ImpactIndicator.AchievedValue</c>.
    ///
    /// Keeping one writer is the point of this class. The execution-side performance columns are
    /// written by four competing implementations (PerformanceService, MonitoringService,
    /// PlanService and a private copy inside IndicatorsController) that disagree on rounding and
    /// on weighted vs. unweighted averaging. Impact indicators do not repeat that.
    ///
    /// This service deliberately never touches project.performance, DisbursementPerformance, or
    /// any framework roll-up: impact measurement is a parallel track.
    /// </summary>
    public class ImpactIndicatorService
    {
        private readonly ApplicationDbContext _context;

        public ImpactIndicatorService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Recalculates the cumulative achieved value for one indicator from its achievements.
        /// Call after every create, edit or delete of an <c>ImpactAchievement</c>.
        /// </summary>
        public async Task RecalculateAsync(int impactIndicatorId)
        {
            var indicator = await _context.ImpactIndicators
                .Include(i => i.Achievements)
                .FirstOrDefaultAsync(i => i.Id == impactIndicatorId);

            if (indicator == null) return;

            indicator.AchievedValue = indicator.Achievements.Sum(a => a.Value);
            await _context.SaveChangesAsync();
        }
    }
}
