using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Infrastructure
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }

        public PermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }

    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly ILogger<PermissionAuthorizationHandler> _logger;

        public PermissionAuthorizationHandler(ILogger<PermissionAuthorizationHandler> logger)
        {
            _logger = logger;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            // Debug, not Information: this runs on EVERY authorization check. At Information
            // it wrote the username and full role list to the persisted stdout log on every
            // request - a PII trail plus real throughput cost.
            _logger.LogDebug("Checking permission {Permission}", requirement.Permission);

            if (context.User.Identity?.IsAuthenticated != true)
            {
                return Task.CompletedTask;
            }

            var userRoles = context.User.Claims
                .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            if (PermissionMap.HasPermission(userRoles, requirement.Permission))
            {
                context.Succeed(requirement);
            }
            else
            {
                // Denials stay at Warning - they are the useful security signal - but use
                // structured parameters so the identity can be filtered out of log sinks.
                _logger.LogWarning("Permission denied for {Permission}. User: {User}",
                    requirement.Permission, context.User.Identity?.Name);
            }

            return Task.CompletedTask;
        }

    }
}
