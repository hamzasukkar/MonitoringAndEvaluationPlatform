using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Services
{
    /// <inheritdoc cref="IMinistryScopeService"/>
    public class MinistryScopeService : IMinistryScopeService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MinistryScopeService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<(bool IsAdmin, int? MinistryCode)> GetScopeAsync()
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            if (principal is null)
            {
                return (false, null);
            }

            if (principal.IsInRole(UserRoles.SystemAdministrator))
            {
                return (true, null);
            }

            var user = await _userManager.GetUserAsync(principal);
            return (false, user?.MinistryCode);
        }

        public async Task<IQueryable<Framework>> ApplyFrameworkScopeAsync(IQueryable<Framework> query)
        {
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (isAdmin) return query;

            // Fail closed: no ministry means no rows, never all rows.
            return scopedMinistryCode is null
                ? query.Where(_ => false)
                : query.Where(f => f.MinistryCode == scopedMinistryCode);
        }

        public async Task<IQueryable<Project>> ApplyProjectScopeAsync(IQueryable<Project> query)
        {
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (isAdmin) return query;

            return scopedMinistryCode is null
                ? query.Where(_ => false)
                : query.Where(p => p.MinistryCode == scopedMinistryCode);
        }

        public Task<bool> ProjectBelongsToScopeAsync(int projectId) =>
            CheckAsync(ministryCode => _context.Projects
                .Where(p => p.ProjectID == projectId)
                .AnyAsync(p => p.MinistryCode == ministryCode));

        public Task<bool> SubOutputBelongsToScopeAsync(int subOutputCode) =>
            CheckAsync(ministryCode => _context.SubOutputs
                .Where(s => s.Code == subOutputCode)
                .AnyAsync(s => s.Output.Outcome.Framework.MinistryCode == ministryCode));

        public Task<bool> ProjectPhaseBelongsToScopeAsync(int projectPhaseId) =>
            CheckAsync(ministryCode => _context.ProjectPhases
                .Where(pp => pp.Id == projectPhaseId)
                .AnyAsync(pp => pp.Project.MinistryCode == ministryCode));

        public Task<bool> MeasureBelongsToScopeAsync(int measureCode) =>
            CheckAsync(ministryCode => _context.Measures
                .Where(m => m.Code == measureCode)
                .AnyAsync(m => m.ProjectPhase.Project.MinistryCode == ministryCode));

        public Task<bool> ActionPlanBelongsToScopeAsync(int actionPlanCode) =>
            CheckAsync(ministryCode => _context.ActionPlans
                .Where(ap => ap.Code == actionPlanCode)
                .AnyAsync(ap => ap.ProjectPhase.Project.MinistryCode == ministryCode));

        public Task<bool> PlanBelongsToScopeAsync(int planCode) =>
            CheckAsync(ministryCode => _context.Plans
                .Where(p => p.Code == planCode)
                .AnyAsync(p => p.ActionPlan.ProjectPhase.Project.MinistryCode == ministryCode));

        /// <summary>
        /// Shared admin/fail-closed gate so each ownership check only has to express its
        /// navigation path to Project.MinistryCode.
        /// </summary>
        private async Task<bool> CheckAsync(Func<int, Task<bool>> belongsToMinistry)
        {
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (isAdmin) return true;
            if (scopedMinistryCode is null) return false;

            return await belongsToMinistry(scopedMinistryCode.Value);
        }
    }
}
