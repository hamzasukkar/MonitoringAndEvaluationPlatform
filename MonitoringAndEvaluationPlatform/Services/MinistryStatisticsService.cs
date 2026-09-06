using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Services
{
    /// <summary>
    /// Rolls ministry statistics up in a fixed number of queries regardless of how many
    /// ministries are asked for: one pass over projects, one over strategies, one over impact
    /// outputs. Per-ministry grouping happens in memory afterwards, because a project can belong
    /// to a ministry by either of two edges and that predicate is cheaper to evaluate once per
    /// project than to push into N queries.
    /// </summary>
    public sealed class MinistryStatisticsService : IMinistryStatisticsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrencyConversionService _currencyConversion;

        public MinistryStatisticsService(
            ApplicationDbContext context,
            ICurrencyConversionService currencyConversion)
        {
            _context = context;
            _currencyConversion = currencyConversion;
        }

        /// <summary>
        /// Total recorded as disbursed for a project, in the project's OWN currency — Plan.Realised
        /// carries no currency of its own and inherits the project's. The single definition of
        /// "disbursed" in the platform; pass it to <see cref="CurrencyConverter.Sum"/> to convert.
        /// </summary>
        public static double RealisedOf(Project project) =>
            project.Phases?
                .Where(phase => phase.ActionPlan != null)
                .SelectMany(phase => phase.ActionPlan!.Plans)
                .Sum(plan => (double)plan.Realised) ?? 0;

        public async Task<IReadOnlyList<MinistryStatistics>> GetAsync(
            int? ministryCode = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default)
        {
            var ministriesQuery = _context.Ministries.AsNoTracking();
            if (ministryCode is int only)
            {
                ministriesQuery = ministriesQuery.Where(m => m.Code == only);
            }

            var ministries = await ministriesQuery.ToListAsync(cancellationToken);
            if (ministries.Count == 0)
            {
                return Array.Empty<MinistryStatistics>();
            }

            var codes = ministries.Select(m => m.Code).ToList();

            // ── 1. Projects, by either edge ──────────────────────────────────────────────
            var projects = await ApplyDateOverlap(
                    _context.Projects
                        .AsNoTracking()
                        .Where(p => (p.MinistryCode != null && codes.Contains(p.MinistryCode.Value))
                                    || p.Ministries.Any(m => codes.Contains(m.Code))),
                    fromDate, toDate)
                .Include(p => p.Ministries)
                .Include(p => p.Phases)
                    .ThenInclude(phase => phase.ActionPlan)
                        .ThenInclude(actionPlan => actionPlan!.Plans)
                .ToListAsync(cancellationToken);

            // ── 2. Strategies owned by these ministries ──────────────────────────────────
            // Projected in SQL: the indicator count needs four levels of the results framework,
            // and Include-ing that chain would drag the whole hierarchy into memory to count it.
            var strategies = await _context.Frameworks
                .AsNoTracking()
                .Where(f => f.MinistryCode != null && codes.Contains(f.MinistryCode.Value))
                .Select(f => new
                {
                    MinistryCode = f.MinistryCode!.Value,
                    f.IndicatorsPerformance,
                    f.DisbursementPerformance,
                    IndicatorCount = f.Outcomes
                        .SelectMany(o => o.Outputs)
                        .SelectMany(op => op.SubOutputs)
                        .SelectMany(so => so.Indicators)
                        .Count()
                })
                .ToListAsync(cancellationToken);

            var strategiesByMinistry = strategies
                .GroupBy(s => s.MinistryCode)
                .ToDictionary(g => g.Key, g => g.ToList());

            // ── 3. Impact outputs ────────────────────────────────────────────────────────
            // WeightedAchievementRate reads IndicatorLinks -> ImpactIndicator -> YearlyValues.
            // A missing Include here renders zeros rather than throwing, so the chain is exact.
            var impactOutputs = await _context.ProjectOutputs
                .AsNoTracking()
                .Where(po => po.Ministries.Any(m => codes.Contains(m.Code)))
                .Include(po => po.Ministries)
                .Include(po => po.IndicatorLinks)
                    .ThenInclude(link => link.ImpactIndicator)
                        .ThenInclude(indicator => indicator.YearlyValues)
                .ToListAsync(cancellationToken);

            var converter = await _currencyConversion.GetConverterAsync(cancellationToken);
            var today = DateTime.Now;

            var results = new List<MinistryStatistics>(ministries.Count);

            foreach (var ministry in ministries)
            {
                var code = ministry.Code;

                var ministryProjects = projects.Where(p => BelongsTo(p, code)).ToList();
                var ministryStrategies = strategiesByMinistry.GetValueOrDefault(code) ?? new();
                var ministryOutputs = impactOutputs
                    .Where(po => po.Ministries.Any(m => m.Code == code))
                    .ToList();

                var ratedOutputs = ministryOutputs.Where(po => po.IndicatorLinks.Any()).ToList();

                results.Add(new MinistryStatistics
                {
                    Code = code,
                    DisplayNameAr = ministry.MinistryDisplayName_AR,
                    DisplayNameEn = ministry.MinistryDisplayName_EN,
                    Logo = ministry.Logo,

                    ProjectAverageIndicators = Math.Round(ministry.IndicatorsPerformance, 2),
                    ProjectAverageDisbursement = Math.Round(ministry.DisbursementPerformance, 2),
                    StrategyRollupIndicators = ministryStrategies.Count > 0
                        ? Math.Round(ministryStrategies.Average(s => s.IndicatorsPerformance), 2)
                        : null,
                    StrategyRollupDisbursement = ministryStrategies.Count > 0
                        ? Math.Round(ministryStrategies.Average(s => s.DisbursementPerformance), 2)
                        : null,

                    Budget = converter.SumBudget(ministryProjects),
                    Disbursed = converter.Sum(ministryProjects, RealisedOf),

                    StrategyCount = ministryStrategies.Count,
                    IndicatorCount = ministryStrategies.Sum(s => s.IndicatorCount),
                    ProjectCount = ministryProjects.Count,
                    ActiveProjectCount = ministryProjects.Count(p => p.EndDate >= today),
                    CompletedProjectCount = ministryProjects.Count(p => p.EndDate < today),

                    ImpactOutputCount = ministryOutputs.Count,
                    ImpactIndicatorCount = ministryOutputs
                        .SelectMany(po => po.IndicatorLinks)
                        .Select(link => link.ImpactIndicatorId)
                        .Distinct()
                        .Count(),
                    ImpactWeightedAchievement = ratedOutputs.Count > 0
                        ? Math.Round(ratedOutputs.Average(po => po.WeightedAchievementRate), 2)
                        : null,

                    LinkageMismatchCount = ministryProjects.Count(p => IsLinkageMismatch(p, code))
                });
            }

            return results;
        }

        public async Task<IReadOnlyDictionary<int, IReadOnlyList<Project>>> GetProjectsByMinistryAsync(
            IEnumerable<int> ministryCodes,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default)
        {
            var codes = ministryCodes.Distinct().ToList();
            if (codes.Count == 0)
            {
                return new Dictionary<int, IReadOnlyList<Project>>();
            }

            var projects = await ApplyDateOverlap(
                    _context.Projects
                        .AsNoTracking()
                        .Where(p => (p.MinistryCode != null && codes.Contains(p.MinistryCode.Value))
                                    || p.Ministries.Any(m => codes.Contains(m.Code))),
                    fromDate, toDate)
                .Include(p => p.Ministries)
                .Include(p => p.Sector)
                .Include(p => p.Phases)
                    .ThenInclude(phase => phase.ActionPlan)
                        .ThenInclude(actionPlan => actionPlan!.Plans)
                .OrderByDescending(p => p.performance)
                .ToListAsync(cancellationToken);

            var byMinistry = codes.ToDictionary(code => code, _ => new List<Project>());
            foreach (var project in projects)
            {
                foreach (var code in codes)
                {
                    if (BelongsTo(project, code))
                    {
                        byMinistry[code].Add(project);
                    }
                }
            }

            return byMinistry.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<Project>)pair.Value);
        }

        /// <summary>
        /// A project belongs to a ministry if EITHER edge says so. The FK is what authorization
        /// and every project list read; the many-to-many is what the stored Ministry.*Performance
        /// columns read. Honouring only one would make this page contradict the other.
        /// </summary>
        private static bool BelongsTo(Project project, int ministryCode) =>
            project.MinistryCode == ministryCode
            || project.Ministries.Any(m => m.Code == ministryCode);

        private static bool IsLinkageMismatch(Project project, int ministryCode) =>
            (project.MinistryCode == ministryCode)
            != project.Ministries.Any(m => m.Code == ministryCode);

        /// <summary>
        /// Overlap, not containment: a project running 2023–2027 belongs in a 2025 report, which
        /// is what "projects in this period" is normally taken to mean.
        /// </summary>
        private static IQueryable<Project> ApplyDateOverlap(
            IQueryable<Project> query, DateTime? fromDate, DateTime? toDate)
        {
            if (fromDate.HasValue)
            {
                query = query.Where(p => p.EndDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(p => p.StartDate <= toDate.Value);
            }

            return query;
        }
    }
}
