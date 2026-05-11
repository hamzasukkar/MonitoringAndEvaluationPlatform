using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;

public class PlanService
{
    private readonly ApplicationDbContext _context;

    public PlanService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task UpdatePlanAsync(Plan plan)
    {
        // Load plan → actionPlan → projectPhase → project
        var existingPlan = await _context.Plans
            .Include(p => p.ActionPlan)
            .ThenInclude(ap => ap.ProjectPhase)
            .ThenInclude(pp => pp.Project)
            .FirstOrDefaultAsync(p => p.Code == plan.Code);

        if (existingPlan == null)
            throw new Exception("Plan not found.");

        existingPlan.Realised = plan.Realised;
        _context.Plans.Update(existingPlan);
        await _context.SaveChangesAsync();

        var project = existingPlan.ActionPlan.ProjectPhase.Project;

        // Update all performance types for this project (cascades through phases)
        var monitoringService = new MonitoringService(_context);
        await monitoringService.UpdateDisbursementPerformancesForProject(project.ProjectID);
    }

    public async Task RecalculatePerformanceAfterProjectDeletion(Project deletedProject)
    {
        // Indicators now directly reference the project; after deletion they become unlinked.
        var affectedSubOutputCodes = await _context.Indicators
            .Where(i => i.ProjectID == deletedProject.ProjectID)
            .Select(i => i.SubOutputCode)
            .Distinct()
            .ToListAsync();

        if (!affectedSubOutputCodes.Any())
            return;

        var monitoringService = new MonitoringService(_context);
        // Cascade upward from each affected subOutput
        foreach (var subOutputCode in affectedSubOutputCodes.Where(c => c != 0))
        {
            // SubOutput performance will be recalculated after indicators are cleared
        }

        await _context.SaveChangesAsync();
    }

    public async Task RecalculatePerformanceAfterIndicatorDeletion(Indicator deletedIndicator)
    {
        var subOutputCode = deletedIndicator.SubOutputCode;
        if (subOutputCode == 0) return;

        await RecalculateParentEntitiesPerformance(subOutputCode);
    }

    private async Task RecalculateParentEntitiesPerformance(int subOutputCode)
    {
        var subOutput = await _context.SubOutputs
            .Include(so => so.Indicators)
            .FirstOrDefaultAsync(so => so.Code == subOutputCode);

        if (subOutput == null) return;

        var indicatorsWithProjects = subOutput.Indicators
            .Where(i => i.ProjectID != null)
            .ToList();

        subOutput.IndicatorsPerformance = indicatorsWithProjects.Any()
            ? indicatorsWithProjects.Average(i => i.IndicatorsPerformance)
            : 0;
        subOutput.DisbursementPerformance = indicatorsWithProjects.Any()
            ? indicatorsWithProjects.Average(i => i.DisbursementPerformance)
            : 0;

        _context.SubOutputs.Update(subOutput);
        await _context.SaveChangesAsync();

        // Cascade to Output
        if (subOutput.OutputCode != 0)
        {
            var output = await _context.Outputs
                .Include(o => o.SubOutputs)
                .FirstOrDefaultAsync(o => o.Code == subOutput.OutputCode);

            if (output != null)
            {
                var activeSubOutputs = output.SubOutputs.Where(so => so.Indicators.Any(i => i.ProjectID != null)).ToList();
                output.IndicatorsPerformance = activeSubOutputs.Any() ? activeSubOutputs.Average(so => so.IndicatorsPerformance) : 0;
                output.DisbursementPerformance = activeSubOutputs.Any() ? activeSubOutputs.Average(so => so.DisbursementPerformance) : 0;
                _context.Outputs.Update(output);
                await _context.SaveChangesAsync();

                if (output.OutcomeCode != 0)
                {
                    var outcome = await _context.Outcomes
                        .Include(oc => oc.Outputs)
                        .FirstOrDefaultAsync(oc => oc.Code == output.OutcomeCode);

                    if (outcome != null)
                    {
                        var activeOutputs = outcome.Outputs.Where(o => o.SubOutputs != null).ToList();
                        outcome.IndicatorsPerformance = activeOutputs.Any() ? activeOutputs.Average(o => o.IndicatorsPerformance) : 0;
                        outcome.DisbursementPerformance = activeOutputs.Any() ? activeOutputs.Average(o => o.DisbursementPerformance) : 0;
                        _context.Outcomes.Update(outcome);
                        await _context.SaveChangesAsync();

                        if (outcome.FrameworkCode != 0)
                        {
                            var framework = await _context.Frameworks
                                .Include(f => f.Outcomes)
                                .FirstOrDefaultAsync(f => f.Code == outcome.FrameworkCode);

                            if (framework != null)
                            {
                                framework.IndicatorsPerformance = framework.Outcomes.Any() ? framework.Outcomes.Average(oc => oc.IndicatorsPerformance) : 0;
                                framework.DisbursementPerformance = framework.Outcomes.Any() ? framework.Outcomes.Average(oc => oc.DisbursementPerformance) : 0;
                                _context.Frameworks.Update(framework);
                                await _context.SaveChangesAsync();
                            }
                        }
                    }
                }
            }
        }
    }

    public async Task UpdateRelatedEntitiesAfterProjectEdit(Project project)
    {
        var monitoringService = new MonitoringService(_context);
        await monitoringService.UpdateMinistryPerformance(project.ProjectID);
        await monitoringService.UpdateSectorPerformance(project.ProjectID);
        await monitoringService.UpdateDonorPerformance(project.ProjectID);
    }
}
