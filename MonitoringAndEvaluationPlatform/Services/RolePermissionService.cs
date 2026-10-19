using MonitoringAndEvaluationPlatform.Infrastructure;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Services
{
    public static class RolePermissionService
    {
        public static Dictionary<string, List<string>> GetPermissionCategories()
        {
            return new Dictionary<string, List<string>>
            {
                ["Login"] = new() { Permissions.Login, Permissions.RecoverPassword },
                ["Strategy Management"] = new() { Permissions.ReadStrategies, Permissions.AddStrategy, Permissions.ModifyStrategy, Permissions.DeleteStrategy },
                ["Policy Management"] = new() { Permissions.ReadPolicies, Permissions.AddPolicy, Permissions.ModifyPolicy, Permissions.DeletePolicy },
                ["Program Management"] = new() { Permissions.ReadPrograms, Permissions.AddProgram, Permissions.EditProgram, Permissions.DeleteProgram },
                ["Subprogram Management"] = new() { Permissions.ReadSubprograms, Permissions.AddSubprogram, Permissions.EditSubprogram, Permissions.DeleteSubprogram },
                ["Project Management"] = new() { Permissions.ReadProjects, Permissions.AddProject, Permissions.EditProject, Permissions.DeleteProject },
                ["Project Forms"] = new() { Permissions.ReadProjectForms, Permissions.FillProjectForm, Permissions.EditProjectForm, Permissions.DeleteProjectForm },
                ["Project Metrics"] = new() { Permissions.ReadProjectMetrics, Permissions.AddMetricValue, Permissions.EditMetricValues, Permissions.DeleteMetricValues },
                ["Action Plans"] = new() { Permissions.ReadActionPlans, Permissions.ModifyPlanStatus, Permissions.DeleteActionPlan },
                ["Control Panel"] = new() { Permissions.ViewControlPanel },
                ["Ministries Management"] = new() { Permissions.ReadMinistries, Permissions.CreateMinistry, Permissions.ModifyMinistry, Permissions.DeleteMinistry, Permissions.DisplayMinistryIndicators },
                ["Project Dashboard"] = new() { Permissions.BrowseProjects, Permissions.MonitorPerformance, Permissions.ClassifyProjects },
                ["Strategic Indicators"] = new() { Permissions.DisplayStrategicIndicators, Permissions.ComparePerformance, Permissions.EditStrategyData },
                ["Performance Reporting"] = new() { Permissions.ViewReports, Permissions.AnalyzePerformance, Permissions.ExportReports },
                ["Outcome Management"] = new() { Permissions.ReadOutcomes, Permissions.AddOutcome, Permissions.ModifyOutcome, Permissions.DeleteOutcome },
                ["Output Management"] = new() { Permissions.ReadOutputs, Permissions.AddOutput, Permissions.ModifyOutput, Permissions.DeleteOutput },
                ["SubOutput Management"] = new() { Permissions.ReadSubOutputs, Permissions.AddSubOutput, Permissions.ModifySubOutput, Permissions.DeleteSubOutput },
                ["Indicator Management"] = new() { Permissions.ReadIndicators, Permissions.AddIndicator, Permissions.ModifyIndicator, Permissions.DeleteIndicator, Permissions.IndicatorAnalysis },
                // These four are enforced by the handler but were missing from this dictionary,
                // so GetAllPermissions() under-reported them on the /Admin/Roles screen.
                ["Requests Management"] = new() { Permissions.ReadRequests, Permissions.SubmitRequest, Permissions.ManageRequests, Permissions.DeleteRequest }
            };
        }

        public static List<string> GetAllPermissions()
        {
            return GetPermissionCategories().Values.SelectMany(p => p).ToList();
        }

        public static List<string> GetPermissionsForRole(string roleName)
        {
            var allPermissions = GetAllPermissions();

            // SystemAdministrator has all permissions
            if (roleName == UserRoles.SystemAdministrator)
            {
                return allPermissions;
            }

            var permissions = new List<string>();

            foreach (var permission in allPermissions)
            {
                if (HasPermission(roleName, permission))
                {
                    permissions.Add(permission);
                }
            }

            return permissions;
        }

        public static Dictionary<string, List<string>> GetPermissionsGroupedByCategory(string roleName)
        {
            var categories = GetPermissionCategories();
            var rolePermissions = GetPermissionsForRole(roleName);

            var result = new Dictionary<string, List<string>>();

            foreach (var category in categories)
            {
                var permsInCategory = category.Value.Where(p => rolePermissions.Contains(p)).ToList();
                if (permsInCategory.Any())
                {
                    result[category.Key] = permsInCategory;
                }
            }

            return result;
        }

        /// <summary>
        /// Delegates to PermissionMap so this screen always reports exactly what the
        /// authorization handler enforces. This method used to carry its own copy of the
        /// mapping, which had drifted and under-reported MinistryStrategyManager.
        /// </summary>
        private static bool HasPermission(string roleName, string permission) =>
            PermissionMap.RoleHasPermission(roleName, permission);

        public static string GetRoleDescription(string roleName)
        {
            return roleName switch
            {
                UserRoles.SystemAdministrator => "Full system access with all permissions",
                UserRoles.MinistriesUser => "Ministry-level access for monitoring and analysis",
                UserRoles.DataEntry => "Data entry and form management access",
                UserRoles.ReadingUser => "Read-only access for viewing reports",
                UserRoles.MinistryStrategyManager => "Ministry strategy owner: manages the results framework and projects for their ministry",
                _ => "Unknown role"
            };
        }

        public static string GetRoleIcon(string roleName)
        {
            return roleName switch
            {
                UserRoles.SystemAdministrator => "fa-user-shield",
                UserRoles.MinistriesUser => "fa-building",
                UserRoles.DataEntry => "fa-keyboard",
                UserRoles.ReadingUser => "fa-eye",
                UserRoles.MinistryStrategyManager => "fa-sitemap",
                _ => "fa-user"
            };
        }

        public static string GetRoleBadgeColor(string roleName)
        {
            return roleName switch
            {
                UserRoles.SystemAdministrator => "danger",
                UserRoles.MinistriesUser => "primary",
                UserRoles.DataEntry => "success",
                UserRoles.ReadingUser => "info",
                UserRoles.MinistryStrategyManager => "warning",
                _ => "secondary"
            };
        }
    }
}
