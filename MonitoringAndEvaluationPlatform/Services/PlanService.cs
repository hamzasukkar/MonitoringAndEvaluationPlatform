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

        // Calculate Physical
        double totalPlannedPhysical = actionPlan.Activities.Sum(a => a.Plans.Where(p => p.Activity.ActivityType == ActivityType.Physical).Sum(p => p.Planned));
        double totalRealisedPhysical = actionPlan.Activities.Sum(a => a.Plans.Where(p => p.Activity.ActivityType == ActivityType.Physical).Sum(p => p.Realised));

        if (totalPlannedPhysical > 0)
        {
            project.Physical = (int)((totalRealisedPhysical / totalPlannedPhysical) * 100);
        }
        else
        {
            project.Physical = 0;
        }


        // Calculate Financial
        double totalPlannedFinancial = actionPlan.Activities.Sum(a => a.Plans.Where(p => p.Activity.ActivityType == ActivityType.Financial).Sum(p => p.Planned));
        double totalRealisedFinancial = actionPlan.Activities.Sum(a => a.Plans.Where(p => p.Activity.ActivityType == ActivityType.Financial).Sum(p => p.Realised));

        if (totalPlannedFinancial > 0)
        {
            project.Financial = (int)((totalRealisedFinancial / totalPlannedFinancial) * 100);
        }
        else
        {
            project.Financial = 0;
        }

        _context.Projects.Update(project);
        await _context.SaveChangesAsync();

        // Update Framework, Outcomes, Outputs, SubOutputs, Indicators
        await UpdateRelatedEntities(project);
    }


    private async Task UpdateRelatedEntities(Project project)
    {
        // A. Get all affected indicators through measures
        var affectedIndicatorIds = await _context.Measures
            .Where(m => m.ProjectID == project.ProjectID)
            .Select(m => m.IndicatorCode)
            .Distinct()
            .ToListAsync();

        // B. Calculate DisbursementPerformance for each affected Indicator
        var indicatorsToUpdate = await _context.Indicators
            .Where(i => affectedIndicatorIds.Contains(i.IndicatorCode))
            .Include(i => i.Measures)
                .ThenInclude(m => m.Project)
            .ToListAsync();

        foreach (var indicator in indicatorsToUpdate)
        {
            // Calculate average DisbursementPerformance from all projects linked to this indicator via Measures
            var linkedProjects = indicator.Measures.Select(m => m.Project).Where(p => p != null).ToList();
            if (linkedProjects.Any())
            {
                indicator.DisbursementPerformance = (int)linkedProjects.Average(p => p.DisbursementPerformance);
                indicator.FieldMonitoring = (int)linkedProjects.Average(p => p.FieldMonitoring);
                indicator.ImpactAssessment = (int)linkedProjects.Average(p => p.ImpactAssessment);
            }
        }

        // C. Calculate DisbursementPerformance for SubOutputs
        var affectedSubOutputIds = indicatorsToUpdate.Select(i => i.SubOutputCode).Distinct().Where(id => id!=0).ToList();
        var subOutputsToUpdate = await _context.SubOutputs
            .Where(so => affectedSubOutputIds.Contains(so.Code))
            .Include(so => so.Indicators)
            .ToListAsync();

        foreach (var subOutput in subOutputsToUpdate)
        {
            if (subOutput.Indicators.Any())
            {
                subOutput.DisbursementPerformance = (int)subOutput.Indicators.Average(i => i.DisbursementPerformance);
                subOutput.FieldMonitoring = (int)subOutput.Indicators.Average(i => i.FieldMonitoring);
                subOutput.ImpactAssessment = (int)subOutput.Indicators.Average(i => i.ImpactAssessment);
            }
        }

        // D. Calculate DisbursementPerformance for Outputs
        var affectedOutputIds = subOutputsToUpdate.Select(so => so.OutputCode).Distinct().Where(id => id!=0).ToList();
        var outputsToUpdate = await _context.Outputs
            .Where(o => affectedOutputIds.Contains(o.Code))
            .Include(o => o.SubOutputs)
            .ToListAsync();

        foreach (var output in outputsToUpdate)
        {
            if (output.SubOutputs.Any())
            {
                output.DisbursementPerformance = (int)output.SubOutputs.Average(so => so.DisbursementPerformance);
                output.FieldMonitoring = (int)output.SubOutputs.Average(so => so.FieldMonitoring);
                output.ImpactAssessment = (int)output.SubOutputs.Average(so => so.ImpactAssessment);
            }
        }

        // E. Calculate DisbursementPerformance for Outcomes
        var affectedOutcomeIds = outputsToUpdate.Select(o => o.OutcomeCode).Distinct().Where(id => id!=0).ToList();
        var outcomesToUpdate = await _context.Outcomes
            .Where(oc => affectedOutcomeIds.Contains(oc.Code))
            .Include(oc => oc.Outputs)
            .ToListAsync();

        foreach (var outcome in outcomesToUpdate)
        {
            if (outcome.Outputs.Any())
            {
                outcome.DisbursementPerformance = (int)outcome.Outputs.Average(o => o.DisbursementPerformance);
                outcome.FieldMonitoring = (int)outcome.Outputs.Average(o => o.FieldMonitoring);
                outcome.ImpactAssessment = (int)outcome.Outputs.Average(o => o.ImpactAssessment);
            }
        }

        // F. Calculate DisbursementPerformance for Frameworks
        var affectedFrameworkIds = outcomesToUpdate.Select(oc => oc.FrameworkCode).Distinct().Where(id => id != 0).ToList();
        var frameworksToUpdate = await _context.Frameworks
            .Where(f => affectedFrameworkIds.Contains(f.Code))
            .Include(f => f.Outcomes)
            .ToListAsync();

        foreach (var framework in frameworksToUpdate)
        {
            if (framework.Outcomes.Any())
            {
                framework.DisbursementPerformance = (int)framework.Outcomes.Average(oc => oc.DisbursementPerformance);
                framework.FieldMonitoring = (int)framework.Outcomes.Average(oc => oc.FieldMonitoring);
                framework.ImpactAssessment = (int)framework.Outcomes.Average(oc => oc.ImpactAssessment);
            }
        }

        // G. Save all changes
        await _context.SaveChangesAsync();
    }
}
