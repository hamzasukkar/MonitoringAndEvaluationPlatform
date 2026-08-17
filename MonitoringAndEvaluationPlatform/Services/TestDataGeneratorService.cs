using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Enums;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.ViewModels;

namespace MonitoringAndEvaluationPlatform.Services
{
    /// <inheritdoc />
    public class TestDataGeneratorService : ITestDataGeneratorService
    {
        private readonly ApplicationDbContext _context;
        private readonly MonitoringService _monitoringService;

        public TestDataGeneratorService(ApplicationDbContext context, MonitoringService monitoringService)
        {
            _context = context;
            _monitoringService = monitoringService;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GENERATION
        // ─────────────────────────────────────────────────────────────────────

        public async Task<TestDataGenerationResult> GenerateAsync(GenerateTestDataViewModel config)
        {
            var prefix = config.NamePrefix?.Trim();
            if (string.IsNullOrEmpty(prefix))
                throw new InvalidOperationException("A name prefix is required so generated data can be deleted later.");

            var ministry = await _context.Ministries.FirstOrDefaultAsync(m => m.Code == config.MinistryCode);
            if (ministry == null)
                throw new InvalidOperationException("The selected ministry no longer exists.");

            // Re-check server-side: the view's live estimate is a convenience, not a control.
            if (config.EstimatedRowCount > GenerateTestDataViewModel.MaxRowCount)
            {
                throw new InvalidOperationException(
                    $"This configuration would create about {config.EstimatedRowCount:N0} rows, " +
                    $"which exceeds the {GenerateTestDataViewModel.MaxRowCount:N0} row limit for a single run. " +
                    "Reduce the counts per level, the phases per project, or the project duration.");
            }

            var result = new TestDataGenerationResult();
            var rng = Random.Shared;

            // Project needs both of these as non-nullable cascade FKs, so make sure rows exist.
            var (projectManagerCode, superVisorCode) = await EnsureProjectPrerequisitesAsync(prefix);
            var defaultSector = await _context.Sectors.FirstOrDefaultAsync();

            var generatedPhaseIds = new List<int>();
            var generatedProjectIds = new List<int>();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // ── 1. Framework ──────────────────────────────────────────────
                var frameworkName = string.IsNullOrWhiteSpace(config.FrameworkName)
                    ? $"{prefix}Framework {DateTime.Now:yyyy-MM-dd HH:mm}"
                    : $"{prefix}{config.FrameworkName.Trim()}";

                var framework = new Framework
                {
                    Name = frameworkName,
                    MinistryCode = ministry.Code,
                    IndicatorsPerformance = 0,
                    DisbursementPerformance = 0
                };
                _context.Frameworks.Add(framework);
                await _context.SaveChangesAsync();
                result.Frameworks = 1;

                // ── 2. Outcomes ───────────────────────────────────────────────
                // One SaveChanges per level rather than per row: CreateComprehensive saves per
                // row, which would be thousands of round-trips at these volumes.
                var outcomeWeights = DistributeWeights(config.OutcomesPerFramework);
                var outcomes = new List<Outcome>();
                for (int i = 0; i < config.OutcomesPerFramework; i++)
                {
                    outcomes.Add(new Outcome
                    {
                        Name = $"{prefix}Outcome {i + 1}",
                        FrameworkCode = framework.Code,
                        Weight = outcomeWeights[i],
                        IndicatorsPerformance = 0,
                        DisbursementPerformance = 0
                    });
                }
                _context.Outcomes.AddRange(outcomes);
                await _context.SaveChangesAsync();
                result.Outcomes = outcomes.Count;

                // ── 3. Outputs ────────────────────────────────────────────────
                var outputWeights = DistributeWeights(config.OutputsPerOutcome);
                var outputs = new List<Output>();
                foreach (var outcome in outcomes)
                {
                    for (int i = 0; i < config.OutputsPerOutcome; i++)
                    {
                        outputs.Add(new Output
                        {
                            Name = $"{prefix}Output {outcome.Code}.{i + 1}",
                            OutcomeCode = outcome.Code,
                            Weight = outputWeights[i],
                            IndicatorsPerformance = 0,
                            DisbursementPerformance = 0
                        });
                    }
                }
                _context.Outputs.AddRange(outputs);
                await _context.SaveChangesAsync();
                result.Outputs = outputs.Count;

                // ── 4. Sub-outputs ────────────────────────────────────────────
                var subOutputWeights = DistributeWeights(config.SubOutputsPerOutput);
                var subOutputs = new List<SubOutput>();
                foreach (var output in outputs)
                {
                    for (int i = 0; i < config.SubOutputsPerOutput; i++)
                    {
                        subOutputs.Add(new SubOutput
                        {
                            Name = $"{prefix}Sub-Output {output.Code}.{i + 1}",
                            OutputCode = output.Code,
                            Weight = subOutputWeights[i],
                            IndicatorsPerformance = 0,
                            DisbursementPerformance = 0
                        });
                    }
                }
                _context.SubOutputs.AddRange(subOutputs);
                await _context.SaveChangesAsync();
                result.SubOutputs = subOutputs.Count;

                // ── 5. Indicators ─────────────────────────────────────────────
                var indicatorWeights = DistributeWeights(config.IndicatorsPerSubOutput);
                var indicators = new List<Indicator>();
                foreach (var subOutput in subOutputs)
                {
                    for (int i = 0; i < config.IndicatorsPerSubOutput; i++)
                    {
                        indicators.Add(new Indicator
                        {
                            Name = $"{prefix}Indicator {subOutput.Code}.{i + 1}",
                            SubOutputCode = subOutput.Code,
                            Weight = indicatorWeights[i],
                            Target = 100,
                            Active = true,
                            IsCommon = false,
                            TargetYear = DateTime.Now.AddYears(1),
                            // All of these are non-nullable columns under <Nullable>enable</Nullable>.
                            Source = "Generated test data",
                            Concept = string.Empty,
                            Description = string.Empty,
                            MethodOfComputation = string.Empty,
                            Comment = string.Empty,
                            IndicatorsPerformance = 0,
                            DisbursementPerformance = 0
                        });
                    }
                }
                _context.Indicators.AddRange(indicators);
                await _context.SaveChangesAsync();
                result.Indicators = indicators.Count;

                if (config.CreateProjectPerIndicator && indicators.Count > 0)
                {
                    // ── 6. Projects ───────────────────────────────────────────
                    var startDate = DateTime.Today.AddMonths(-config.ProjectDurationMonths / 2);
                    var endDate = startDate.AddMonths(config.ProjectDurationMonths);

                    var projects = new List<Project>();
                    for (int i = 0; i < indicators.Count; i++)
                    {
                        projects.Add(new Project
                        {
                            // Scoped by framework code so re-runs with the same prefix never collide.
                            ProjectName = $"{prefix}Project {framework.Code}-{i + 1}",
                            EstimatedBudget = rng.Next(500_000, 5_000_000),
                            RealBudget = 0,
                            // SYP keeps ExchangeRate legitimately null under RequiredWhenCurrencyNotSyp.
                            Currency = "SYP",
                            ExchangeRate = null,
                            BudgetUnit = BudgetUnit.Ones,
                            StartDate = startDate,
                            EndDate = endDate,
                            ProjectManagerCode = projectManagerCode,
                            SuperVisorCode = superVisorCode,
                            MinistryCode = ministry.Code,
                            IsEntireCountry = true,
                            performance = 0,
                            DisbursementPerformance = 0
                        });
                    }
                    _context.Projects.AddRange(projects);
                    await _context.SaveChangesAsync();
                    result.Projects = projects.Count;
                    generatedProjectIds.AddRange(projects.Select(p => p.ProjectID));

                    // Link each indicator to its project, and attach the M2M lookups so the
                    // project shows up in ministry- and sector-filtered views.
                    for (int i = 0; i < indicators.Count; i++)
                    {
                        indicators[i].ProjectID = projects[i].ProjectID;
                        projects[i].Ministries.Add(ministry);
                        if (defaultSector != null)
                        {
                            projects[i].Sectors!.Add(defaultSector);
                        }
                    }
                    await _context.SaveChangesAsync();

                    // ── 7. Phases ─────────────────────────────────────────────
                    var phaseNames = ProjectPhase.DefaultCategoryNames.Take(config.PhasesPerProject).ToArray();
                    var phaseWeights = DistributeWeightsDecimal(phaseNames.Length);

                    var phases = new List<ProjectPhase>();
                    foreach (var project in projects)
                    {
                        for (int i = 0; i < phaseNames.Length; i++)
                        {
                            phases.Add(new ProjectPhase
                            {
                                Name = phaseNames[i],
                                ProjectID = project.ProjectID,
                                StartDate = project.StartDate,
                                EndDate = project.EndDate,
                                // Phase weight is a 0-100 percentage summing to 100 per project.
                                Weight = phaseWeights[i],
                                Budget = project.EstimatedBudget * (double)phaseWeights[i] / 100.0,
                                PhasePerformance = 0
                            });
                        }
                    }
                    _context.ProjectPhases.AddRange(phases);
                    await _context.SaveChangesAsync();
                    result.Phases = phases.Count;
                    generatedPhaseIds.AddRange(phases.Select(p => p.Id));

                    // ── 8. Action plans (one per phase — unique index on ProjectPhaseId) ──
                    var actionPlans = phases.Select(phase => new ActionPlan
                    {
                        ProjectPhaseId = phase.Id,
                        PlansCount = CountMonths(phase.StartDate, phase.EndDate)
                    }).ToList();
                    _context.ActionPlans.AddRange(actionPlans);
                    await _context.SaveChangesAsync();
                    result.ActionPlans = actionPlans.Count;

                    // ── 9. Monthly plans ──────────────────────────────────────
                    var plans = new List<Plan>();
                    for (int i = 0; i < phases.Count; i++)
                    {
                        var phase = phases[i];
                        var actionPlanCode = actionPlans[i].Code;

                        var months = EnumerateMonths(phase.StartDate, phase.EndDate).ToList();

                        // Disbursement % = Σ Realised ÷ project EstimatedBudget. Phase budgets sum
                        // to the project budget, so scaling each phase by the same factor lands the
                        // project on that factor — a believable partial disbursement.
                        long[] realisedPerMonth = config.PopulateValues
                            ? SplitAmount(phase.Budget * (0.30 + rng.NextDouble() * 0.60), months.Count, rng)
                            : new long[months.Count];

                        for (int m = 0; m < months.Count; m++)
                        {
                            plans.Add(new Plan
                            {
                                Name = $"Plan {m + 1}",
                                Date = months[m],
                                Realised = realisedPerMonth[m],
                                ActionPlanCode = actionPlanCode
                            });
                        }
                    }
                    _context.Plans.AddRange(plans);
                    await _context.SaveChangesAsync();
                    result.Plans = plans.Count;

                    // ── 10. Measures ──────────────────────────────────────────
                    if (config.PopulateValues && config.MeasuresPerPhase > 0)
                    {
                        var measures = new List<Measure>();
                        foreach (var phase in phases)
                        {
                            var span = (phase.EndDate - phase.StartDate).TotalDays;

                            // MonitoringService.UpdatePhasePerformance SUMS a phase's measure
                            // values into PhasePerformance, so a phase's values must together
                            // total a sensible percentage rather than each being one on its own.
                            double phaseTarget = 30 + (rng.NextDouble() * 65);
                            var values = DistributeAscending(config.MeasuresPerPhase, phaseTarget, rng);

                            for (int m = 0; m < config.MeasuresPerPhase; m++)
                            {
                                measures.Add(new Measure
                                {
                                    Name = $"{prefix}Measure {m + 1}",
                                    ProjectPhaseId = phase.Id,
                                    Date = phase.StartDate.AddDays(span * (m + 1) / (config.MeasuresPerPhase + 1)),
                                    // Column is constrained to 0-100.
                                    Value = Math.Clamp(values[m], 0, 100),
                                    MeasureType = MeasureType.Qualitative
                                });
                            }
                        }
                        _context.Measures.AddRange(measures);
                        await _context.SaveChangesAsync();
                        result.Measures = measures.Count;
                    }
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            // Recalculation runs after the commit because MonitoringService issues its own
            // SaveChanges calls and would otherwise be enlisted in the transaction above.
            if (config.PopulateValues)
            {
                foreach (var phaseId in generatedPhaseIds)
                {
                    await _monitoringService.UpdatePhasePerformance(phaseId);
                }

                foreach (var projectId in generatedProjectIds)
                {
                    await _monitoringService.UpdateProjectPerformanceFromPhases(projectId);
                    // One call per project — this cascades project → indicators → sub-output →
                    // output → outcome → framework → ministry/sector/donor.
                    await _monitoringService.UpdateDisbursementPerformancesForProject(projectId);
                }
            }

            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETION
        // ─────────────────────────────────────────────────────────────────────

        public async Task<TestDataGenerationResult> DeleteByPrefixAsync(string prefix)
        {
            prefix = prefix?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(prefix))
                throw new InvalidOperationException("A prefix is required. Refusing to delete with an empty prefix.");

            var result = new TestDataGenerationResult();

            var frameworks = await _context.Frameworks
                .Where(f => f.Name.StartsWith(prefix))
                .ToListAsync();

            var frameworkCodes = frameworks.Select(f => f.Code).ToList();

            var outcomes = await _context.Outcomes
                .Where(o => frameworkCodes.Contains(o.FrameworkCode))
                .ToListAsync();
            var outcomeCodes = outcomes.Select(o => o.Code).ToList();

            var outputs = await _context.Outputs
                .Where(o => outcomeCodes.Contains(o.OutcomeCode))
                .ToListAsync();
            var outputCodes = outputs.Select(o => o.Code).ToList();

            var subOutputs = await _context.SubOutputs
                .Where(s => outputCodes.Contains(s.OutputCode))
                .ToListAsync();
            var subOutputCodes = subOutputs.Select(s => s.Code).ToList();

            var indicators = await _context.Indicators
                .Where(i => subOutputCodes.Contains(i.SubOutputCode))
                .ToListAsync();

            // Indicator → Project is DeleteBehavior.SetNull, so cascading the framework would
            // leave every generated project orphaned. Collect them explicitly, and sweep up any
            // prefixed project that lost its indicator link.
            var projectIds = indicators
                .Where(i => i.ProjectID.HasValue)
                .Select(i => i.ProjectID!.Value)
                .ToList();

            var strayProjectIds = await _context.Projects
                .Where(p => p.ProjectName.StartsWith(prefix))
                .Select(p => p.ProjectID)
                .ToListAsync();

            projectIds = projectIds.Union(strayProjectIds).ToList();

            var phases = await _context.ProjectPhases
                .Where(p => projectIds.Contains(p.ProjectID))
                .ToListAsync();
            var phaseIds = phases.Select(p => p.Id).ToList();

            var actionPlans = await _context.ActionPlans
                .Where(a => phaseIds.Contains(a.ProjectPhaseId))
                .ToListAsync();
            var actionPlanCodes = actionPlans.Select(a => a.Code).ToList();

            var plans = await _context.Plans
                .Where(p => actionPlanCodes.Contains(p.ActionPlanCode))
                .ToListAsync();

            var measures = await _context.Measures
                .Where(m => phaseIds.Contains(m.ProjectPhaseId))
                .ToListAsync();

            if (frameworks.Count == 0 && projectIds.Count == 0)
            {
                return result;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // FK-safe order, mirroring DataManagementService.DeleteProjectsAsync.
                _context.Plans.RemoveRange(plans);
                result.Plans = plans.Count;

                _context.ActionPlans.RemoveRange(actionPlans);
                result.ActionPlans = actionPlans.Count;

                _context.Measures.RemoveRange(measures);
                result.Measures = measures.Count;

                _context.ProjectPhases.RemoveRange(phases);
                result.Phases = phases.Count;
                await _context.SaveChangesAsync();

                var projectDonors = await _context.ProjectDonors
                    .Where(pd => projectIds.Contains(pd.ProjectId))
                    .ToListAsync();
                _context.ProjectDonors.RemoveRange(projectDonors);

                var projectFiles = await _context.ProjectFiles
                    .Where(pf => projectIds.Contains(pf.ProjectId))
                    .ToListAsync();
                _context.ProjectFiles.RemoveRange(projectFiles);
                await _context.SaveChangesAsync();

                // Break the indicator → project link before deleting projects, otherwise the
                // SetNull cascade fires mid-delete against rows we are about to remove anyway.
                foreach (var indicator in indicators)
                {
                    indicator.ProjectID = null;
                }
                await _context.SaveChangesAsync();

                // Join-table rows have no entity class; clear the skip navigations instead.
                var projects = await _context.Projects
                    .Where(p => projectIds.Contains(p.ProjectID))
                    .Include(p => p.Sectors)
                    .Include(p => p.Ministries)
                    .Include(p => p.Donors)
                    .Include(p => p.Governorates)
                    .Include(p => p.Districts)
                    .Include(p => p.SubDistricts)
                    .Include(p => p.Communities)
                    .ToListAsync();

                foreach (var project in projects)
                {
                    project.Sectors?.Clear();
                    project.Ministries.Clear();
                    project.Donors?.Clear();
                    project.Governorates.Clear();
                    project.Districts.Clear();
                    project.SubDistricts.Clear();
                    project.Communities.Clear();
                }
                await _context.SaveChangesAsync();

                _context.Projects.RemoveRange(projects);
                result.Projects = projects.Count;
                await _context.SaveChangesAsync();

                _context.Indicators.RemoveRange(indicators);
                result.Indicators = indicators.Count;

                _context.SubOutputs.RemoveRange(subOutputs);
                result.SubOutputs = subOutputs.Count;

                _context.Outputs.RemoveRange(outputs);
                result.Outputs = outputs.Count;

                _context.Outcomes.RemoveRange(outcomes);
                result.Outcomes = outcomes.Count;

                _context.Frameworks.RemoveRange(frameworks);
                result.Frameworks = frameworks.Count;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Project.ProjectManagerCode and SuperVisorCode are required cascade FKs. Reuse the first
        /// existing row of each, or create a prefixed placeholder.
        /// </summary>
        private async Task<(int projectManagerCode, int superVisorCode)> EnsureProjectPrerequisitesAsync(string prefix)
        {
            var projectManager = await _context.ProjectManagers.FirstOrDefaultAsync();
            if (projectManager == null)
            {
                projectManager = new ProjectManager
                {
                    Name = $"{prefix}Project Manager",
                    PhoneNumber = "0000000000",
                    Email = "test.manager@example.com"
                };
                _context.ProjectManagers.Add(projectManager);
                await _context.SaveChangesAsync();
            }

            var superVisor = await _context.SuperVisors.FirstOrDefaultAsync();
            if (superVisor == null)
            {
                superVisor = new SuperVisor
                {
                    Name = $"{prefix}Supervisor",
                    PhoneNumber = "0000000000",
                    Email = "test.supervisor@example.com"
                };
                _context.SuperVisors.Add(superVisor);
                await _context.SaveChangesAsync();
            }

            return (projectManager.Code, superVisor.Code);
        }

        /// <summary>
        /// Equal weights summing to exactly 100, rounding remainder onto the last sibling —
        /// the same result the controllers' RedistributeWeights produces, but with no extra
        /// queries, so generated trees look hand-entered in the AdjustWeights UI.
        /// </summary>
        private static double[] DistributeWeights(int count)
        {
            if (count <= 0) return Array.Empty<double>();

            var weights = new double[count];
            double equal = Math.Round(100.0 / count, 2);
            for (int i = 0; i < count; i++) weights[i] = equal;

            double total = equal * count;
            if (Math.Abs(total - 100.0) > 0.001)
            {
                weights[count - 1] += 100.0 - total;
            }
            return weights;
        }

        /// <summary>Decimal variant for ProjectPhase.Weight, which is decimal(5,2).</summary>
        private static decimal[] DistributeWeightsDecimal(int count)
        {
            if (count <= 0) return Array.Empty<decimal>();

            var weights = new decimal[count];
            decimal equal = Math.Round(100m / count, 2);
            for (int i = 0; i < count; i++) weights[i] = equal;

            decimal total = equal * count;
            if (total != 100m)
            {
                weights[count - 1] += 100m - total;
            }
            return weights;
        }

        /// <summary>Month count between two dates, clamped to at least 1 (matches ProjectsController).</summary>
        private static int CountMonths(DateTime start, DateTime end)
        {
            int months = ((end.Year - start.Year) * 12) + end.Month - start.Month;
            if (end.Day < start.Day) months--;
            return months <= 0 ? 1 : months;
        }

        /// <summary>First-of-month dates from start through end, inclusive of both.</summary>
        private static IEnumerable<DateTime> EnumerateMonths(DateTime start, DateTime end)
        {
            var current = new DateTime(start.Year, start.Month, 1);
            var last = new DateTime(end.Year, end.Month, 1);
            while (current <= last)
            {
                yield return current;
                current = current.AddMonths(1);
            }
        }

        /// <summary>
        /// Values that rise across the series and sum to <paramref name="total"/>, so a phase's
        /// measures read as progress over time while still totalling a believable percentage.
        /// </summary>
        private static double[] DistributeAscending(int count, double total, Random rng)
        {
            if (count <= 0) return Array.Empty<double>();

            var raw = new double[count];
            double sum = 0;
            for (int i = 0; i < count; i++)
            {
                raw[i] = (i + 1) + rng.NextDouble();
                sum += raw[i];
            }

            var values = new double[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = Math.Round(total * raw[i] / sum, 2);
            }
            return values;
        }

        /// <summary>Splits an amount across n buckets with jitter, preserving the total.</summary>
        private static long[] SplitAmount(double total, int buckets, Random rng)
        {
            if (buckets <= 0) return Array.Empty<long>();

            var shares = new double[buckets];
            double sum = 0;
            for (int i = 0; i < buckets; i++)
            {
                shares[i] = 0.5 + rng.NextDouble();
                sum += shares[i];
            }

            var amounts = new long[buckets];
            for (int i = 0; i < buckets; i++)
            {
                amounts[i] = (long)Math.Round(total * shares[i] / sum);
            }
            return amounts;
        }
    }
}
