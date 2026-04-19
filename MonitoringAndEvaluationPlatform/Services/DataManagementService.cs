using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.ViewModels.DataManagement;
using System.Text;

namespace MonitoringAndEvaluationPlatform.Services
{
    public class DataManagementService : IDataManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly PlanService _planService;

        public DataManagementService(ApplicationDbContext context, PlanService planService)
        {
            _context = context;
            _planService = planService;
        }

        public async Task<DataManagementIndexViewModel> GetIndexStatisticsAsync()
        {
            return new DataManagementIndexViewModel
            {
                TotalProjects = await _context.Projects.CountAsync(),
                TotalFrameworks = await _context.Frameworks.CountAsync(),
                TotalIndicators = await _context.Indicators.CountAsync(),
                TotalPlans = await _context.Plans.CountAsync(),
                TotalMeasures = await _context.Measures.CountAsync(),
                TotalAuditLogs = await _context.AuditLogs.CountAsync()
            };
        }

        public async Task<string> GenerateDatabaseBackupScriptAsync(bool includeIdentityData, bool includeAuditLogs)
        {
            var sb = new StringBuilder();
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            sb.AppendLine("-- ================================================");
            sb.AppendLine($"-- Database Backup Script");
            sb.AppendLine($"-- Generated: {timestamp} UTC");
            sb.AppendLine("-- Monitoring and Evaluation Platform");
            sb.AppendLine("-- ================================================");
            sb.AppendLine();
            sb.AppendLine("SET NOCOUNT ON;");
            sb.AppendLine();
            sb.AppendLine("-- Disable all constraints");
            sb.AppendLine("EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';");
            sb.AppendLine();

            // Define tables to backup in order (respecting FK dependencies)
            var tablesToBackup = new List<string>
            {
                "Frameworks", "Outcomes", "Outputs", "SubOutputs", "Indicators",
                "Goals", "Targets", "sDGIndicators",
                "Ministries", "Sectors", "Donors", "SuperVisors", "ProjectManagers",
                "Governorates", "Districts", "SubDistricts", "Communities",
                "Projects", "ProjectPhases", "ActionPlans", "Activities", "Plans", "Measures",
                "ProjectDonors", "ProjectFiles",
                "FrameworkGoals", "FrameworkGoalYearlyValues", "FrameworkGoalFiles"
            };

            if (includeAuditLogs)
            {
                tablesToBackup.Add("AuditLogs");
            }

            foreach (var tableName in tablesToBackup)
            {
                await GenerateTableBackupScript(sb, tableName);
            }

            sb.AppendLine();
            sb.AppendLine("-- Re-enable all constraints");
            sb.AppendLine("EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';");
            sb.AppendLine();
            sb.AppendLine("-- End of backup script");

            return sb.ToString();
        }

        private async Task GenerateTableBackupScript(StringBuilder sb, string tableName)
        {
            sb.AppendLine($"-- Table: {tableName}");
            sb.AppendLine($"PRINT 'Processing {tableName}...';");

            try
            {
                // Get table data dynamically
                var entityType = _context.Model.GetEntityTypes()
                    .FirstOrDefault(e => e.GetTableName()?.Equals(tableName, StringComparison.OrdinalIgnoreCase) == true);

                if (entityType == null)
                {
                    sb.AppendLine($"-- Table {tableName} not found in model");
                    sb.AppendLine();
                    return;
                }

                var clrType = entityType.ClrType;
                var dbSetProperty = _context.GetType().GetProperties()
                    .FirstOrDefault(p => p.PropertyType.IsGenericType &&
                                        p.PropertyType.GetGenericArguments().FirstOrDefault() == clrType);

                if (dbSetProperty == null)
                {
                    sb.AppendLine($"-- DbSet for {tableName} not found");
                    sb.AppendLine();
                    return;
                }

                var dbSet = dbSetProperty.GetValue(_context);
                if (dbSet == null)
                {
                    sb.AppendLine($"-- DbSet value for {tableName} is null");
                    sb.AppendLine();
                    return;
                }

                // Get columns
                var properties = entityType.GetProperties()
                    .Where(p => !p.IsShadowProperty())
                    .ToList();

                var columnNames = string.Join(", ", properties.Select(p => $"[{p.GetColumnName()}]"));

                // Check for identity column
                var identityProp = properties.FirstOrDefault(p => p.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd);
                if (identityProp != null)
                {
                    sb.AppendLine($"SET IDENTITY_INSERT [{tableName}] ON;");
                }

                // Get actual data using raw SQL for simplicity
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                try
                {
                    using var command = connection.CreateCommand();
                    command.CommandText = $"SELECT COUNT(*) FROM [{tableName}]";
                    var count = Convert.ToInt32(await command.ExecuteScalarAsync());

                    if (count > 0)
                    {
                        command.CommandText = $"SELECT * FROM [{tableName}]";
                        using var reader = await command.ExecuteReaderAsync();

                        while (await reader.ReadAsync())
                        {
                            var values = new List<string>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                values.Add(FormatSqlValue(reader.GetValue(i)));
                            }

                            var colNames = string.Join(", ", Enumerable.Range(0, reader.FieldCount)
                                .Select(i => $"[{reader.GetName(i)}]"));
                            sb.AppendLine($"INSERT INTO [{tableName}] ({colNames}) VALUES ({string.Join(", ", values)});");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"-- No data in {tableName}");
                    }
                }
                finally
                {
                    await connection.CloseAsync();
                }

                if (identityProp != null)
                {
                    sb.AppendLine($"SET IDENTITY_INSERT [{tableName}] OFF;");
                }

                sb.AppendLine();
            }
            catch (Exception ex)
            {
                sb.AppendLine($"-- Error backing up {tableName}: {ex.Message}");
                sb.AppendLine();
            }
        }

        private string FormatSqlValue(object value)
        {
            if (value == null || value == DBNull.Value)
                return "NULL";

            return value switch
            {
                string s => $"N'{s.Replace("'", "''")}'",
                DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss.fff}'",
                DateTimeOffset dto => $"'{dto:yyyy-MM-dd HH:mm:ss.fff zzz}'",
                bool b => b ? "1" : "0",
                decimal d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
                double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
                float f => f.ToString(System.Globalization.CultureInfo.InvariantCulture),
                _ => value.ToString() ?? "NULL"
            };
        }

        public async Task<ClearDataViewModel> GetTableGroupsAsync()
        {
            var groups = new List<TableGroupViewModel>
            {
                new TableGroupViewModel
                {
                    GroupKey = "performance",
                    GroupName = "Performance Data",
                    Description = "Plans and Measures tables containing performance tracking data",
                    DangerLevel = "Medium",
                    Tables = new List<string> { "Plans", "Measures" },
                    TotalRecords = await _context.Plans.CountAsync() + await _context.Measures.CountAsync()
                },
                new TableGroupViewModel
                {
                    GroupKey = "projects",
                    GroupName = "Project Data",
                    Description = "Projects with associated action plans, activities, files, and relationships",
                    DangerLevel = "High",
                    Tables = new List<string> { "Projects", "ActionPlans", "ProjectDonors", "ProjectFiles" },
                    TotalRecords = await _context.Projects.CountAsync() + await _context.ActionPlans.CountAsync() +
                                   await _context.ProjectDonors.CountAsync() + await _context.ProjectFiles.CountAsync()
                },
                new TableGroupViewModel
                {
                    GroupKey = "framework",
                    GroupName = "Framework Structure",
                    Description = "Frameworks, Outcomes, Outputs, SubOutputs, Indicators and Framework Goals",
                    DangerLevel = "Critical",
                    Tables = new List<string> { "Frameworks", "Outcomes", "Outputs", "SubOutputs", "Indicators", "FrameworkGoals", "FrameworkGoalYearlyValues", "FrameworkGoalFiles" },
                    TotalRecords = await _context.Frameworks.CountAsync() + await _context.Outcomes.CountAsync() +
                                   await _context.Outputs.CountAsync() + await _context.SubOutputs.CountAsync() +
                                   await _context.Indicators.CountAsync() + await _context.FrameworkGoals.CountAsync() +
                                   await _context.FrameworkGoalYearlyValues.CountAsync() + await _context.FrameworkGoalFiles.CountAsync()
                },
                new TableGroupViewModel
                {
                    GroupKey = "reference",
                    GroupName = "Reference Data",
                    Description = "Donors, Sectors, Ministries, Supervisors, and Project Managers",
                    DangerLevel = "High",
                    Tables = new List<string> { "Donors", "Sectors", "Ministries", "SuperVisors", "ProjectManagers" },
                    TotalRecords = await _context.Donors.CountAsync() + await _context.Sectors.CountAsync() +
                                   await _context.Ministries.CountAsync() + await _context.SuperVisors.CountAsync() +
                                   await _context.ProjectManagers.CountAsync()
                },
                new TableGroupViewModel
                {
                    GroupKey = "location",
                    GroupName = "Location Data",
                    Description = "Governorates, Districts, SubDistricts, and Communities",
                    DangerLevel = "High",
                    Tables = new List<string> { "Governorates", "Districts", "SubDistricts", "Communities" },
                    TotalRecords = await _context.Governorates.CountAsync() + await _context.Districts.CountAsync() +
                                   await _context.SubDistricts.CountAsync() + await _context.Communities.CountAsync()
                },
                new TableGroupViewModel
                {
                    GroupKey = "sdg",
                    GroupName = "SDG Data",
                    Description = "Sustainable Development Goals, Targets, and SDG Indicators",
                    DangerLevel = "Medium",
                    Tables = new List<string> { "Goals", "Targets", "sDGIndicators" },
                    TotalRecords = await _context.Goals.CountAsync() + await _context.Targets.CountAsync() +
                                   await _context.sDGIndicators.CountAsync()
                },
                new TableGroupViewModel
                {
                    GroupKey = "audit",
                    GroupName = "Audit Logs",
                    Description = "System audit trail and activity logs",
                    DangerLevel = "Low",
                    Tables = new List<string> { "AuditLogs" },
                    TotalRecords = await _context.AuditLogs.CountAsync()
                }
            };

            return new ClearDataViewModel { TableGroups = groups };
        }

        public async Task<int> ClearTablesAsync(List<string> groupKeys)
        {
            int totalDeleted = 0;

            // Order matters - delete dependent tables first
            var deleteOrder = new List<(string GroupKey, string[] Tables)>
            {
                ("audit", new[] { "AuditLogs" }),
                ("performance", new[] { "Plans", "Measures" }),
                ("projects", new[] { "ProjectFiles", "ProjectDonors", "ProjectIndicators", "Plans", "ActionPlans", "Projects" }),
                ("sdg", new[] { "sDGIndicators", "Targets", "Goals" }),
                ("framework", new[] { "FrameworkGoalFiles", "FrameworkGoalYearlyValues", "FrameworkGoals", "Indicators", "SubOutputs", "Outputs", "Outcomes", "Frameworks" }),
                ("reference", new[] { "ProjectManagers", "SuperVisors", "Ministries", "Sectors", "Donors" }),
                ("location", new[] { "Communities", "SubDistricts", "Districts", "Governorates" })
            };

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Disable FK constraints
                await _context.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'");

                foreach (var (groupKey, tables) in deleteOrder)
                {
                    if (groupKeys.Contains(groupKey))
                    {
                        foreach (var table in tables)
                        {
                            var deleted = await _context.Database.ExecuteSqlRawAsync($"DELETE FROM [{table}]");
                            totalDeleted += deleted;
                        }
                    }
                }

                // Re-enable FK constraints
                await _context.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL'");

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return totalDeleted;
        }

        public async Task<DeleteProjectViewModel> GetProjectsForDeletionAsync(string? searchTerm = null)
        {
            var query = _context.Projects
                .Include(p => p.Phases)
                    .ThenInclude(pp => pp.ActionPlan)
                        .ThenInclude(ap => ap.Plans)
                .Include(p => p.Indicators)
                .Include(p => p.ProjectFiles)
                .Include(p => p.Ministry)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p => p.ProjectName.Contains(searchTerm));
            }

            var projects = await query
                .OrderBy(p => p.ProjectName)
                .Select(p => new ProjectForDeletionViewModel
                {
                    ProjectId = p.ProjectID,
                    ProjectName = p.ProjectName,
                    ActionPlanCount = p.Phases.Count(pp => pp.ActionPlan != null),
                    ActivityCount = 0,
                    PlanCount = p.Phases.Where(pp => pp.ActionPlan != null).SelectMany(pp => pp.ActionPlan!.Plans).Count(),
                    IndicatorCount = p.Indicators.Count,
                    FileCount = p.ProjectFiles.Count,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    MinistryName = p.Ministry != null ? p.Ministry.MinistryDisplayName_EN : null
                })
                .ToListAsync();

            return new DeleteProjectViewModel
            {
                Projects = projects,
                SearchTerm = searchTerm
            };
        }

        public async Task<int> DeleteProjectsAsync(List<int> projectIds)
        {
            int totalDeleted = 0;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var projectId in projectIds)
                {
                    var project = await _context.Projects
                        .Include(p => p.Phases)
                            .ThenInclude(pp => pp.ActionPlan)
                                .ThenInclude(ap => ap.Plans)
                        .Include(p => p.Indicators)
                        .Include(p => p.ProjectDonors)
                        .Include(p => p.ProjectFiles)
                        .Include(p => p.Sectors)
                        .Include(p => p.Ministries)
                        .Include(p => p.Governorates)
                        .Include(p => p.Districts)
                        .Include(p => p.SubDistricts)
                        .Include(p => p.Communities)
                        .FirstOrDefaultAsync(p => p.ProjectID == projectId);

                    if (project == null) continue;

                    // Store indicator codes for recalculation
                    var indicatorCodes = project.Indicators.Select(i => i.IndicatorCode).ToList();

                    // Delete Plans and ActionPlans from all phases
                    foreach (var phase in project.Phases)
                    {
                        if (phase.ActionPlan != null)
                        {
                            _context.Plans.RemoveRange(phase.ActionPlan.Plans);
                            totalDeleted += phase.ActionPlan.Plans.Count;
                            _context.ActionPlans.Remove(phase.ActionPlan);
                            totalDeleted++;
                        }
                    }
                    _context.ProjectPhases.RemoveRange(project.Phases);
                    totalDeleted += project.Phases.Count;

                    // Delete related data
                    _context.ProjectDonors.RemoveRange(project.ProjectDonors);
                    _context.ProjectFiles.RemoveRange(project.ProjectFiles);
                    totalDeleted += project.ProjectDonors.Count + project.ProjectFiles.Count;

                    // Clear many-to-many relationships
                    project.Sectors?.Clear();
                    project.Ministries?.Clear();
                    project.Governorates?.Clear();
                    project.Districts?.Clear();
                    project.SubDistricts?.Clear();
                    project.Communities?.Clear();

                    // Delete project
                    _context.Projects.Remove(project);
                    totalDeleted++;

                    await _context.SaveChangesAsync();

                    // Recalculate performance for affected indicators
                    foreach (var indicatorCode in indicatorCodes)
                    {
                        var indicator = await _context.Indicators.FindAsync(indicatorCode);
                        if (indicator != null)
                        {
                            await _planService.RecalculatePerformanceAfterIndicatorDeletion(indicator);
                        }
                    }
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return totalDeleted;
        }

        public async Task<ResetValuesViewModel> GetResetValuesStatisticsAsync()
        {
            return new ResetValuesViewModel
            {
                TotalPlans = await _context.Plans.CountAsync(),
                TotalMeasures = await _context.Measures.CountAsync(),
                TotalProjects = await _context.Projects.CountAsync()
            };
        }

        public async Task<int> ResetValuesAsync(bool resetPlans, bool resetMeasures, bool resetProjectPerformance, bool recalculateHierarchy)
        {
            int totalReset = 0;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (resetPlans)
                {
                    totalReset += await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE [Plans] SET Planned = 0, Realised = 0");
                }

                if (resetMeasures)
                {
                    totalReset += await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE [Measures] SET Value = 0");
                }

                if (resetProjectPerformance)
                {
                    totalReset += await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE [Projects] SET performance = 0, DisbursementPerformance = 0");

                    totalReset += await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE [Indicators] SET DisbursementPerformance = 0");

                    totalReset += await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE [SubOutputs] SET DisbursementPerformance = 0");

                    totalReset += await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE [Outputs] SET DisbursementPerformance = 0");

                    totalReset += await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE [Outcomes] SET DisbursementPerformance = 0");

                    totalReset += await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE [Frameworks] SET DisbursementPerformance = 0");

                    totalReset += await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE [Donors] SET DisbursementPerformance = 0");

                    totalReset += await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE [Sectors] SET DisbursementPerformance = 0");

                    totalReset += await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE [Ministries] SET DisbursementPerformance = 0");
                }

                if (recalculateHierarchy && !resetProjectPerformance)
                {
                    // Get all indicators and recalculate performance hierarchy
                    var allIndicators = await _context.Indicators.ToListAsync();
                    foreach (var indicator in allIndicators)
                    {
                        await _planService.RecalculatePerformanceAfterIndicatorDeletion(indicator);
                    }
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return totalReset;
        }
    }
}
