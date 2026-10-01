using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Services
{
    public sealed class MinistryScopeService : IMinistryScopeService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        // Scoped lifetime: one resolution per request, shared by every controller, view and
        // partial that asks.
        private MinistryScope? _cached;

        public MinistryScopeService(
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _context = context;
        }

        public async Task<MinistryScope> GetScopeAsync(CancellationToken cancellationToken = default)
        {
            if (_cached is not null) return _cached;

            var principal = _httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
                return _cached = MinistryScope.Nothing;

            if (principal.IsInRole(UserRoles.SystemAdministrator))
                return _cached = MinistryScope.Unrestricted;

            // Read from the database rather than a claim, so a ministry change made by an
            // administrator takes effect on the user's next request, not their next login.
            var userId = _userManager.GetUserId(principal);
            int? ministryCode = userId is null
                ? null
                : await _context.Users
                    .AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => u.MinistryCode)
                    .FirstOrDefaultAsync(cancellationToken);

            return _cached = new MinistryScope(false, ministryCode);
        }

        public Task<bool> CanAccessMinistryAsync(int ministryCode, CancellationToken cancellationToken = default) =>
            AnyInScopeAsync(s => _context.Ministries.Where(m => m.Code == ministryCode).WithinScope(s), cancellationToken);

        public Task<bool> CanAccessProjectAsync(int projectId, CancellationToken cancellationToken = default) =>
            AnyInScopeAsync(s => _context.Projects.Where(p => p.ProjectID == projectId).WithinScope(s), cancellationToken);

        public Task<bool> CanAccessPhaseAsync(int phaseId, CancellationToken cancellationToken = default) =>
            AnyInScopeAsync(s => _context.ProjectPhases.Where(ph => ph.Id == phaseId).WithinScope(s), cancellationToken);

        public Task<bool> CanAccessPhasesAsync(IReadOnlyCollection<int> phaseIds, CancellationToken cancellationToken = default) =>
            AllInScopeAsync(phaseIds, (s, ids) => _context.ProjectPhases.Where(ph => ids.Contains(ph.Id)).WithinScope(s), cancellationToken);

        public Task<bool> CanAccessMeasureAsync(int measureCode, CancellationToken cancellationToken = default) =>
            AnyInScopeAsync(s => _context.Measures.Where(m => m.Code == measureCode).WithinScope(s), cancellationToken);

        public Task<bool> CanAccessActionPlanAsync(int actionPlanCode, CancellationToken cancellationToken = default) =>
            AnyInScopeAsync(s => _context.ActionPlans.Where(ap => ap.Code == actionPlanCode).WithinScope(s), cancellationToken);

        public Task<bool> CanAccessPlansAsync(IReadOnlyCollection<int> planCodes, CancellationToken cancellationToken = default) =>
            AllInScopeAsync(planCodes, (s, ids) => _context.Plans.Where(pl => ids.Contains(pl.Code)).WithinScope(s), cancellationToken);

        public Task<bool> CanAccessFrameworkAsync(int frameworkCode, CancellationToken cancellationToken = default) =>
            AnyInScopeAsync(s => _context.Frameworks.Where(f => f.Code == frameworkCode).WithinScope(s), cancellationToken);

        public Task<bool> CanAccessOutcomeAsync(int outcomeCode, CancellationToken cancellationToken = default) =>
            AnyInScopeAsync(s => _context.Outcomes.Where(o => o.Code == outcomeCode).WithinScope(s), cancellationToken);

        public Task<bool> CanAccessOutputAsync(int outputCode, CancellationToken cancellationToken = default) =>
            AnyInScopeAsync(s => _context.Outputs.Where(o => o.Code == outputCode).WithinScope(s), cancellationToken);

        public Task<bool> CanAccessSubOutputAsync(int subOutputCode, CancellationToken cancellationToken = default) =>
            AnyInScopeAsync(s => _context.SubOutputs.Where(so => so.Code == subOutputCode).WithinScope(s), cancellationToken);

        public Task<bool> CanAccessIndicatorAsync(int indicatorCode, CancellationToken cancellationToken = default) =>
            AnyInScopeAsync(s => _context.Indicators.Where(i => i.IndicatorCode == indicatorCode).WithinScope(s), cancellationToken);

        private async Task<bool> AnyInScopeAsync<T>(
            Func<MinistryScope, IQueryable<T>> query,
            CancellationToken cancellationToken)
        {
            var scope = await GetScopeAsync(cancellationToken);
            if (scope.IsAdmin) return true;
            if (scope.SeesNothing) return false;
            return await query(scope).AnyAsync(cancellationToken);
        }

        // All-or-nothing: one foreign or missing id in the set denies the whole request.
        private async Task<bool> AllInScopeAsync<T>(
            IReadOnlyCollection<int> ids,
            Func<MinistryScope, List<int>, IQueryable<T>> query,
            CancellationToken cancellationToken)
        {
            var scope = await GetScopeAsync(cancellationToken);
            if (scope.IsAdmin) return true;
            if (scope.SeesNothing) return false;

            var distinct = ids.Distinct().ToList();
            if (distinct.Count == 0) return true;
            return await query(scope, distinct).CountAsync(cancellationToken) == distinct.Count;
        }
    }
}
