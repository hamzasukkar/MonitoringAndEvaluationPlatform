using System.Linq.Expressions;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Services
{
    /// <summary>
    /// One owner expression per ministry-owned entity. An administrator's query is returned
    /// unchanged; anyone else sees only rows whose owner equals their ministry, and a user with
    /// no ministry sees nothing.
    ///
    /// Projects (and everything hanging off them) are owned by <c>Project.MinistryCode</c> alone:
    /// the ProjectMinistries list is a back-compat mirror and never grants access. The strategy
    /// tree is owned by <c>Framework.MinistryCode</c>.
    /// </summary>
    public static class MinistryScopeQueryExtensions
    {
        public static IQueryable<Ministry> WithinScope(this IQueryable<Ministry> query, MinistryScope scope) =>
            Restrict(query, scope, own => m => m.Code == own);

        public static IQueryable<Project> WithinScope(this IQueryable<Project> query, MinistryScope scope) =>
            Restrict(query, scope, own => p => p.MinistryCode == own);

        public static IQueryable<ProjectPhase> WithinScope(this IQueryable<ProjectPhase> query, MinistryScope scope) =>
            Restrict(query, scope, own => ph => ph.Project.MinistryCode == own);

        public static IQueryable<Measure> WithinScope(this IQueryable<Measure> query, MinistryScope scope) =>
            Restrict(query, scope, own => m => m.ProjectPhase.Project.MinistryCode == own);

        public static IQueryable<ActionPlan> WithinScope(this IQueryable<ActionPlan> query, MinistryScope scope) =>
            Restrict(query, scope, own => ap => ap.ProjectPhase.Project.MinistryCode == own);

        public static IQueryable<Plan> WithinScope(this IQueryable<Plan> query, MinistryScope scope) =>
            Restrict(query, scope, own => pl => pl.ActionPlan.ProjectPhase.Project.MinistryCode == own);

        public static IQueryable<ImpactIndicator> WithinScope(this IQueryable<ImpactIndicator> query, MinistryScope scope) =>
            Restrict(query, scope, own => ii => ii.Project.MinistryCode == own);

        public static IQueryable<Framework> WithinScope(this IQueryable<Framework> query, MinistryScope scope) =>
            Restrict(query, scope, own => f => f.MinistryCode == own);

        public static IQueryable<FrameworkGoal> WithinScope(this IQueryable<FrameworkGoal> query, MinistryScope scope) =>
            Restrict(query, scope, own => g => g.Framework.MinistryCode == own);

        public static IQueryable<Outcome> WithinScope(this IQueryable<Outcome> query, MinistryScope scope) =>
            Restrict(query, scope, own => o => o.Framework.MinistryCode == own);

        public static IQueryable<Output> WithinScope(this IQueryable<Output> query, MinistryScope scope) =>
            Restrict(query, scope, own => o => o.Outcome.Framework.MinistryCode == own);

        public static IQueryable<SubOutput> WithinScope(this IQueryable<SubOutput> query, MinistryScope scope) =>
            Restrict(query, scope, own => s => s.Output.Outcome.Framework.MinistryCode == own);

        /// <summary>By the strategy the indicator sits under.</summary>
        public static IQueryable<Indicator> WithinScope(this IQueryable<Indicator> query, MinistryScope scope) =>
            Restrict(query, scope, own => i => i.SubOutput.Output.Outcome.Framework.MinistryCode == own);

        public static IQueryable<Request> WithinScope(this IQueryable<Request> query, MinistryScope scope) =>
            Restrict(query, scope, own => r => r.MinistryCode == own);

        // `own` is captured by the returned lambda, so EF sends it as a parameter and caches one
        // plan per entity rather than one per ministry.
        private static IQueryable<T> Restrict<T>(
            IQueryable<T> query,
            MinistryScope scope,
            Func<int, Expression<Func<T, bool>>> ownedBy)
        {
            if (scope.IsAdmin) return query;
            return scope.MinistryCode is int own
                ? query.Where(ownedBy(own))
                : query.Where(_ => false);
        }
    }
}
