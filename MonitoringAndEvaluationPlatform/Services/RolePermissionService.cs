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
                ["Indicator Management"] = new() { Permissions.ReadIndicators, Permissions.AddIndicator, Permissions.ModifyIndicator, Permissions.DeleteIndicator, Permissions.IndicatorAnalysis }
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

        private static bool HasPermission(string roleName, string permission)
        {
            // Mirrors the logic from PermissionAuthorizationHandler
            return permission switch
            {
                // Login permissions - all users
                Permissions.Login or Permissions.RecoverPassword => true,

                // Strategy Management
                Permissions.ReadStrategies => roleName is UserRoles.SystemAdministrator or UserRoles.MinistriesUser or UserRoles.DataEntry or UserRoles.ReadingUser,
                Permissions.AddStrategy or Permissions.ModifyStrategy or Permissions.DeleteStrategy => roleName == UserRoles.SystemAdministrator,

                // Policy Management
                Permissions.ReadPolicies => roleName is UserRoles.SystemAdministrator or UserRoles.MinistriesUser or UserRoles.DataEntry or UserRoles.ReadingUser,
                Permissions.AddPolicy or Permissions.ModifyPolicy or Permissions.DeletePolicy => roleName == UserRoles.SystemAdministrator,

                // Program Management
                Permissions.ReadPrograms => roleName is UserRoles.SystemAdministrator or UserRoles.MinistriesUser or UserRoles.DataEntry or UserRoles.ReadingUser,
                Permissions.AddProgram or Permissions.EditProgram or Permissions.DeleteProgram => roleName == UserRoles.SystemAdministrator,

                // Subprogram Management
                Permissions.ReadSubprograms => roleName is UserRoles.SystemAdministrator or UserRoles.MinistriesUser or UserRoles.DataEntry or UserRoles.ReadingUser,
                Permissions.AddSubprogram or Permissions.EditSubprogram or Permissions.DeleteSubprogram => roleName == UserRoles.SystemAdministrator,

                // Project Management
                Permissions.ReadProjects => roleName is UserRoles.SystemAdministrator or UserRoles.MinistriesUser or UserRoles.DataEntry,
                Permissions.AddProject or Permissions.EditProject or Permissions.DeleteProject => roleName is UserRoles.SystemAdministrator or UserRoles.MinistriesUser or UserRoles.DataEntry,

                // Project Forms
                Permissions.ReadProjectForms => roleName is UserRoles.SystemAdministrator or UserRoles.MinistriesUser or UserRoles.DataEntry,
                Permissions.FillProjectForm or Permissions.EditProjectForm or Permissions.DeleteProjectForm => roleName == UserRoles.DataEntry,

                // Project Metrics
                Permissions.ReadProjectMetrics => roleName is UserRoles.SystemAdministrator or UserRoles.MinistriesUser or UserRoles.DataEntry,
                Permissions.AddMetricValue or Permissions.EditMetricValues or Permissions.DeleteMetricValues => roleName == UserRoles.DataEntry,

                // Action Plans
                Permissions.ReadActionPlans => roleName is UserRoles.SystemAdministrator or UserRoles.MinistriesUser or UserRoles.DataEntry,
                Permissions.ModifyPlanStatus or Permissions.DeleteActionPlan => roleName == UserRoles.DataEntry,

                // Control Panel
                Permissions.ViewControlPanel => roleName is UserRoles.SystemAdministrator or UserRoles.MinistriesUser or UserRoles.DataEntry or UserRoles.ReadingUser,

                // Ministries Management
                Permissions.ReadMinistries => roleName is UserRoles.SystemAdministrator or UserRoles.MinistriesUser or UserRoles.DataEntry or UserRoles.ReadingUser,
                Permissions.CreateMinistry or Permissions.ModifyMinistry or Permissions.DeleteMinistry or Permissions.DisplayMinistryIndicators => roleName == UserRoles.SystemAdministrator,

                // Project Dashboard
                Permissions.BrowseProjects or Permissions.ClassifyProjects => roleName is UserRoles.SystemAdministrator or UserRoles.MinistriesUser or UserRoles.DataEntry or UserRoles.ReadingUser,
                Permissions.MonitorPerformance => roleName is UserRoles.SystemAdministrator or UserRoles.MinistriesUser,

                // Strategic Indicators Dashboard
                Permissions.DisplayStrategicIndicators => roleName is UserRoles.SystemAdministrator or UserRoles.MinistriesUser or UserRoles.DataEntry or UserRoles.ReadingUser,
                Permissions.ComparePerformance => roleName is UserRoles.SystemAdministrator or UserRoles.MinistriesUser or UserRoles.DataEntry,
                Permissions.EditStrategyData => roleName == UserRoles.SystemAdministrator,

                // Performance Reporting
                Permissions.ViewReports => roleName is UserRoles.SystemAdministrator or UserRoles.MinistriesUser or UserRoles.ReadingUser,
                Permissions.AnalyzePerformance or Permissions.ExportReports => roleName is UserRoles.SystemAdministrator or UserRoles.MinistriesUser,

                // Outcome Management
                Permissions.ReadOutcomes => roleName is UserRoles.SystemAdministrator or UserRoles.MinistriesUser or UserRoles.DataEntry or UserRoles.ReadingUser,
                Permissions.AddOutcome or Permissions.ModifyOutcome or Permissions.DeleteOutcome => roleName == UserRoles.SystemAdministrator,

                // Output Management
                Permissions.ReadOutputs => roleName is UserRoles.SystemAdministrator or UserRoles.MinistriesUser or UserRoles.DataEntry or UserRoles.ReadingUser,
                Permissions.AddOutput or Permissions.ModifyOutput or Permissions.DeleteOutput => roleName == UserRoles.SystemAdministrator,

                // SubOutput Management
                Permissions.ReadSubOutputs => roleName is UserRoles.SystemAdministrator or UserRoles.MinistriesUser or UserRoles.DataEntry or UserRoles.ReadingUser,
                Permissions.AddSubOutput or Permissions.ModifySubOutput or Permissions.DeleteSubOutput => roleName == UserRoles.SystemAdministrator,

                // Indicator Management
                Permissions.ReadIndicators => roleName is UserRoles.SystemAdministrator or UserRoles.MinistriesUser or UserRoles.DataEntry or UserRoles.ReadingUser,
                Permissions.AddIndicator or Permissions.ModifyIndicator or Permissions.DeleteIndicator => roleName == UserRoles.SystemAdministrator,
                Permissions.IndicatorAnalysis => roleName is UserRoles.SystemAdministrator or UserRoles.MinistriesUser,

                // Requests Management
                Permissions.ReadRequests or Permissions.SubmitRequest => roleName is UserRoles.SystemAdministrator or UserRoles.MinistriesUser or UserRoles.DataEntry,
                Permissions.ManageRequests or Permissions.DeleteRequest => roleName == UserRoles.SystemAdministrator,

                _ => false
            };
        }

        public static string GetRoleDescription(string roleName)
        {
            return roleName switch
            {
                UserRoles.SystemAdministrator => "Full system access with all permissions",
                UserRoles.MinistriesUser => "Ministry-level access for monitoring and analysis",
                UserRoles.DataEntry => "Data entry and form management access",
                UserRoles.ReadingUser => "Read-only access for viewing reports",
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
                _ => "secondary"
            };
        }
    }
}
