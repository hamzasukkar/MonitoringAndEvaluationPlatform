using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Enums;
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
        var existingPlan = await _context.Plans
            .Include(p => p.Activity)
            .ThenInclude(a => a.ActionPlan)
            .ThenInclude(ap => ap.Project)
            .FirstOrDefaultAsync(p => p.Code == plan.Code);

        if (existingPlan == null)
        {
            throw new Exception("Plan not found.");
        }

        // Update Plan values
        existingPlan.Planned = plan.Planned;
        existingPlan.Realised = plan.Realised;
        _context.Plans.Update(existingPlan);
        await _context.SaveChangesAsync();

        // Update Project and related entities
        await UpdateProjectPerformance(existingPlan.Activity.ActionPlan.Project);
    }

    private async Task UpdateProjectPerformance(Project project)
    {
        if (project == null) return;

        var actionPlan = await _context.ActionPlans
            .Include(ap => ap.Activities)
            .ThenInclude(a => a.Plans)
            .FirstOrDefaultAsync(ap => ap.ProjectID == project.ProjectID);

        if (actionPlan == null) return;

        // Calculate DisbursementPerformance
        double totalPlannedPerformance = actionPlan.Activities.Sum(a => a.Plans.Where(p => p.Activity.ActivityType == ActivityType.DisbursementPerformance).Sum(p => p.Planned));
        double totalRealisedPerformance = actionPlan.Activities.Sum(a => a.Plans.Where(p => p.Activity.ActivityType == ActivityType.DisbursementPerformance).Sum(p => p.Realised));

        if (totalPlannedPerformance > 0)
        {
            project.DisbursementPerformance = (int)((totalRealisedPerformance / totalPlannedPerformance) * 100);
        }
        else
        {
            project.DisbursementPerformance = 0;
        }

        // Calculate FieldMonitoring
        double totalPlannedMonitoring = actionPlan.Activities.Sum(a => a.Plans.Where(p => p.Activity.ActivityType == ActivityType.FieldMonitoring).Sum(p => p.Planned));
        double totalRealisedMonitoring = actionPlan.Activities.Sum(a => a.Plans.Where(p => p.Activity.ActivityType == ActivityType.FieldMonitoring).Sum(p => p.Realised));

        if (totalPlannedMonitoring > 0)
        {
            project.FieldMonitoring = (int)((totalRealisedMonitoring / totalPlannedMonitoring) * 100);
        }
        else
        {
            project.FieldMonitoring = 0;
        }

        // Calculate ImpactAssessment
        double totalPlannedImpact = actionPlan.Activities.Sum(a => a.Plans.Where(p => p.Activity.ActivityType == ActivityType.ImpactAssessment).Sum(p => p.Planned));
        double totalRealisedImpact = actionPlan.Activities.Sum(a => a.Plans.Where(p => p.Activity.ActivityType == ActivityType.ImpactAssessment).Sum(p => p.Realised));

        if (totalPlannedImpact > 0)
        {
            project.ImpactAssessment = (int)((totalRealisedImpact / totalPlannedImpact) * 100);
        }
        else
        {
            project.ImpactAssessment = 0;
        }

        _context.Projects.Update(project);
        await _context.SaveChangesAsync();

        // Update Framework, Outcomes, Outputs, SubOutputs, Indicators
        await UpdateRelatedEntities(project);
    }


    private async Task UpdateRelatedEntities(Project project)
    {
        // A. Fetch all relevant Measures for the current project, including their linked Indicators and the hierarchy above
        var relevantMeasures = await _context.Measures
            .Where(m => m.ProjectID == project.ProjectID)
            .Include(m => m.Indicator)
                .ThenInclude(i => i.SubOutput)
                    .ThenInclude(so => so.Output)
                        .ThenInclude(o => o.Outcome)
                            .ThenInclude(outc => outc.Framework) // Renamed 'o' to 'outc' to avoid conflict
            .ToListAsync();

        // Use HashSets to keep track of unique entities to avoid re-processing and ensure we update each entity only once
        var indicatorsToUpdate = new HashSet<Indicator>();
        var subOutputsToUpdate = new HashSet<SubOutput>();
        var outputsToUpdate = new HashSet<Output>();
        var outcomesToUpdate = new HashSet<Outcome>();
        var frameworksToUpdate = new HashSet<Framework>();

        // B. Propagate performance from Project to Indicator (and collect unique entities)
        foreach (var measure in relevantMeasures)
        {
            var indicator = measure.Indicator;
            if (indicator != null)
            {
                // For simplicity, let's assume direct propagation of project performance to indicators
                // In a real scenario, indicator performance would be derived from its measures,
                // then aggregated up.
                indicator.DisbursementPerformance = project.DisbursementPerformance;
                indicator.FieldMonitoring = project.FieldMonitoring;
                indicator.ImpactAssessment = project.ImpactAssessment;
                indicatorsToUpdate.Add(indicator); // Add to set for tracking
            }
        }

        // C. Aggregate performance upwards from Indicator to Framework
        // This is where the weights would typically come into play for a more accurate calculation.
        // For this example, I'll still do a direct propagation for simplicity, but note the
        // comment about weighted average.

        // Process SubOutputs
        foreach (var indicator in indicatorsToUpdate)
        {
            var subOutput = indicator.SubOutput;
            if (subOutput != null)
            {
                // In a real scenario, calculate subOutput.DisbursementPerformance
                // as a weighted average of its indicators' DisbursementPerformance.
                // For now, propagating directly from project.
                subOutput.DisbursementPerformance = project.DisbursementPerformance;
                subOutput.FieldMonitoring = project.FieldMonitoring;
                subOutput.ImpactAssessment = project.ImpactAssessment;
                subOutputsToUpdate.Add(subOutput);
            }
        }

        // Process Outputs
        foreach (var subOutput in subOutputsToUpdate)
        {
            var output = subOutput.Output;
            if (output != null)
            {
                // In a real scenario, calculate output.DisbursementPerformance
                // as a weighted average of its subOutputs' DisbursementPerformance.
                output.DisbursementPerformance = project.DisbursementPerformance;
                output.FieldMonitoring = project.FieldMonitoring;
                output.ImpactAssessment = project.ImpactAssessment;
                outputsToUpdate.Add(output);
            }
        }

        // Process Outcomes
        foreach (var output in outputsToUpdate)
        {
            var outcome = output.Outcome;
            if (outcome != null)
            {
                // In a real scenario, calculate outcome.DisbursementPerformance
                // as a weighted average of its outputs' DisbursementPerformance.
                outcome.DisbursementPerformance = project.DisbursementPerformance;
                outcome.FieldMonitoring = project.FieldMonitoring;
                outcome.ImpactAssessment = project.ImpactAssessment;
                outcomesToUpdate.Add(outcome);
            }
        }

        // Process Frameworks
        foreach (var outcome in outcomesToUpdate)
        {
            var framework = outcome.Framework;
            if (framework != null)
            {
                // In a real scenario, calculate framework.DisbursementPerformance
                // as a weighted average of its outcomes' DisbursementPerformance.
                framework.DisbursementPerformance = project.DisbursementPerformance;
                framework.FieldMonitoring = project.FieldMonitoring;
                framework.ImpactAssessment = project.ImpactAssessment;
                frameworksToUpdate.Add(framework);
            }
        }

        // D. Save changes
        // Entity Framework's change tracking will handle saving updates to all entities
        // that have been modified and added to the context.
        await _context.SaveChangesAsync();
    }
}
