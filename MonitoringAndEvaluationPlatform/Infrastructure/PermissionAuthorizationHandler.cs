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
            _logger.LogInformation($"Checking permission: {requirement.Permission}");

            if (context.User.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning("User is not authenticated");
                return Task.CompletedTask;
            }

            var userRoles = context.User.Claims
                .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            _logger.LogInformation($"User roles: {string.Join(", ", userRoles)}");
            _logger.LogInformation($"User identity name: {context.User.Identity?.Name}");
            _logger.LogInformation($"Checking SystemAdministrator: {userRoles.Contains(UserRoles.SystemAdministrator)}");
            _logger.LogInformation($"SystemAdministrator constant: '{UserRoles.SystemAdministrator}'");

            if (HasPermission(userRoles, requirement.Permission))
            {
                _logger.LogInformation($"Permission granted for {requirement.Permission}");
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning($"Permission denied for {requirement.Permission}. User roles: [{string.Join(", ", userRoles)}]");
            }

            return Task.CompletedTask;
        }

        private static bool HasPermission(List<string> userRoles, string permission)
        {
            // System Administrator has all permissions
            if (userRoles.Contains(UserRoles.SystemAdministrator))
            {
                return true;
            }

            // Define role-based permissions based on the PDF document
            return permission switch
            {
                // Login permissions - all users
                Permissions.Login or Permissions.RecoverPassword => true,

                // Strategy Management - Only System Administrator
                Permissions.ReadStrategies => userRoles.Contains(UserRoles.SystemAdministrator) ||
                                            userRoles.Contains(UserRoles.MinistriesUser) ||
                                            userRoles.Contains(UserRoles.DataEntry) ||
                                            userRoles.Contains(UserRoles.ReadingUser),
                Permissions.AddStrategy or Permissions.ModifyStrategy or Permissions.DeleteStrategy =>
                    userRoles.Contains(UserRoles.SystemAdministrator),

                // Policy Management - Only System Administrator
                Permissions.ReadPolicies => userRoles.Contains(UserRoles.SystemAdministrator) ||
                                          userRoles.Contains(UserRoles.MinistriesUser) ||
                                          userRoles.Contains(UserRoles.DataEntry) ||
                                          userRoles.Contains(UserRoles.ReadingUser),
                Permissions.AddPolicy or Permissions.ModifyPolicy or Permissions.DeletePolicy =>
                    userRoles.Contains(UserRoles.SystemAdministrator),

                // Program Management - Only System Administrator
                Permissions.ReadPrograms => userRoles.Contains(UserRoles.SystemAdministrator) ||
                                          userRoles.Contains(UserRoles.MinistriesUser) ||
                                          userRoles.Contains(UserRoles.DataEntry) ||
                                          userRoles.Contains(UserRoles.ReadingUser),
                Permissions.AddProgram or Permissions.EditProgram or Permissions.DeleteProgram =>
                    userRoles.Contains(UserRoles.SystemAdministrator),

                // Subprogram Management - Only System Administrator
                Permissions.ReadSubprograms => userRoles.Contains(UserRoles.SystemAdministrator) ||
                                             userRoles.Contains(UserRoles.MinistriesUser) ||
                                             userRoles.Contains(UserRoles.DataEntry) ||
                                             userRoles.Contains(UserRoles.ReadingUser),
                Permissions.AddSubprogram or Permissions.EditSubprogram or Permissions.DeleteSubprogram =>
                    userRoles.Contains(UserRoles.SystemAdministrator),

                // Project Management - SystemAdmin, MinistriesUser, DataEntry can read; SystemAdmin and DataEntry can modify
                Permissions.ReadProjects => userRoles.Contains(UserRoles.SystemAdministrator) ||
                                          userRoles.Contains(UserRoles.MinistriesUser) ||
                                          userRoles.Contains(UserRoles.DataEntry),
                Permissions.AddProject or Permissions.EditProject or Permissions.DeleteProject =>
                    userRoles.Contains(UserRoles.SystemAdministrator) ||
                    userRoles.Contains(UserRoles.DataEntry),

                // Project Forms - SystemAdmin, MinistriesUser, DataEntry can read; Only DataEntry can modify
                Permissions.ReadProjectForms => userRoles.Contains(UserRoles.SystemAdministrator) ||
                                              userRoles.Contains(UserRoles.MinistriesUser) ||
                                              userRoles.Contains(UserRoles.DataEntry),
                Permissions.FillProjectForm or Permissions.EditProjectForm or Permissions.DeleteProjectForm =>
                    userRoles.Contains(UserRoles.DataEntry),

                // Project Metrics - SystemAdmin, MinistriesUser, DataEntry can read; Only DataEntry can modify
                Permissions.ReadProjectMetrics => userRoles.Contains(UserRoles.SystemAdministrator) ||
                                                 userRoles.Contains(UserRoles.MinistriesUser) ||
                                                 userRoles.Contains(UserRoles.DataEntry),
                Permissions.AddMetricValue or Permissions.EditMetricValues or Permissions.DeleteMetricValues =>
                    userRoles.Contains(UserRoles.DataEntry),

                // Action Plans - SystemAdmin, MinistriesUser, DataEntry can read; Only DataEntry can modify
                Permissions.ReadActionPlans => userRoles.Contains(UserRoles.SystemAdministrator) ||
                                             userRoles.Contains(UserRoles.MinistriesUser) ||
                                             userRoles.Contains(UserRoles.DataEntry),
                Permissions.ModifyPlanStatus or Permissions.DeleteActionPlan =>
                    userRoles.Contains(UserRoles.DataEntry),

                // General Control Panel - All users can view
                Permissions.ViewControlPanel => userRoles.Contains(UserRoles.SystemAdministrator) ||
                                              userRoles.Contains(UserRoles.MinistriesUser) ||
                                              userRoles.Contains(UserRoles.DataEntry) ||
                                              userRoles.Contains(UserRoles.ReadingUser),

                // Ministries Management - Only System Administrator
                Permissions.ReadMinistries => userRoles.Contains(UserRoles.SystemAdministrator) ||
                                            userRoles.Contains(UserRoles.MinistriesUser) ||
                                            userRoles.Contains(UserRoles.DataEntry) ||
                                            userRoles.Contains(UserRoles.ReadingUser),
                Permissions.CreateMinistry or Permissions.ModifyMinistry or Permissions.DeleteMinistry or Permissions.DisplayMinistryIndicators =>
                    userRoles.Contains(UserRoles.SystemAdministrator),

                // Project Dashboard - All except ReadingUser can browse; SystemAdmin and MinistriesUser can monitor
                Permissions.BrowseProjects or Permissions.ClassifyProjects =>
                    userRoles.Contains(UserRoles.SystemAdministrator) ||
                    userRoles.Contains(UserRoles.MinistriesUser) ||
                    userRoles.Contains(UserRoles.DataEntry) ||
                    userRoles.Contains(UserRoles.ReadingUser),
                Permissions.MonitorPerformance =>
                    userRoles.Contains(UserRoles.SystemAdministrator) ||
                    userRoles.Contains(UserRoles.MinistriesUser),

                // Strategic Indicators Dashboard - All can view; SystemAdmin can edit strategy data
                Permissions.DisplayStrategicIndicators =>
                    userRoles.Contains(UserRoles.SystemAdministrator) ||
                    userRoles.Contains(UserRoles.MinistriesUser) ||
                    userRoles.Contains(UserRoles.DataEntry) ||
                    userRoles.Contains(UserRoles.ReadingUser),
                Permissions.ComparePerformance =>
                    userRoles.Contains(UserRoles.SystemAdministrator) ||
                    userRoles.Contains(UserRoles.MinistriesUser) ||
                    userRoles.Contains(UserRoles.DataEntry),
                Permissions.EditStrategyData =>
                    userRoles.Contains(UserRoles.SystemAdministrator),

                // Performance Reporting - SystemAdmin and MinistriesUser can view and analyze; ReadingUser can only view
                Permissions.ViewReports =>
                    userRoles.Contains(UserRoles.SystemAdministrator) ||
                    userRoles.Contains(UserRoles.MinistriesUser) ||
                    userRoles.Contains(UserRoles.ReadingUser),
                Permissions.AnalyzePerformance or Permissions.ExportReports =>
                    userRoles.Contains(UserRoles.SystemAdministrator) ||
                    userRoles.Contains(UserRoles.MinistriesUser),

                // Outcome Management - Same as Strategy Management
                Permissions.ReadOutcomes => userRoles.Contains(UserRoles.SystemAdministrator) ||
                                          userRoles.Contains(UserRoles.MinistriesUser) ||
                                          userRoles.Contains(UserRoles.DataEntry) ||
                                          userRoles.Contains(UserRoles.ReadingUser),
                Permissions.AddOutcome or Permissions.ModifyOutcome or Permissions.DeleteOutcome =>
                    userRoles.Contains(UserRoles.SystemAdministrator),

                // Output Management - Same as Strategy Management
                Permissions.ReadOutputs => userRoles.Contains(UserRoles.SystemAdministrator) ||
                                         userRoles.Contains(UserRoles.MinistriesUser) ||
                                         userRoles.Contains(UserRoles.DataEntry) ||
                                         userRoles.Contains(UserRoles.ReadingUser),
                Permissions.AddOutput or Permissions.ModifyOutput or Permissions.DeleteOutput =>
                    userRoles.Contains(UserRoles.SystemAdministrator),

                // SubOutput Management - Same as Strategy Management
                Permissions.ReadSubOutputs => userRoles.Contains(UserRoles.SystemAdministrator) ||
                                            userRoles.Contains(UserRoles.MinistriesUser) ||
                                            userRoles.Contains(UserRoles.DataEntry) ||
                                            userRoles.Contains(UserRoles.ReadingUser),
                Permissions.AddSubOutput or Permissions.ModifySubOutput or Permissions.DeleteSubOutput =>
                    userRoles.Contains(UserRoles.SystemAdministrator),

                // Indicator Management - Same as Strategy Management
                Permissions.ReadIndicators => userRoles.Contains(UserRoles.SystemAdministrator) ||
                                            userRoles.Contains(UserRoles.MinistriesUser) ||
                                            userRoles.Contains(UserRoles.DataEntry) ||
                                            userRoles.Contains(UserRoles.ReadingUser),
                Permissions.AddIndicator or Permissions.ModifyIndicator or Permissions.DeleteIndicator =>
                    userRoles.Contains(UserRoles.SystemAdministrator),

                // Indicator Analysis - SystemAdmin and MinistriesUser only
                Permissions.IndicatorAnalysis =>
                    userRoles.Contains(UserRoles.SystemAdministrator) ||
                    userRoles.Contains(UserRoles.MinistriesUser),

                _ => false
            };
        }
    }
}