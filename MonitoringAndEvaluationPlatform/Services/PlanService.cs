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
        var frameworks = await _context.Frameworks
            .Include(f => f.Outcomes)
            .ThenInclude(o => o.Outputs)
            .ThenInclude(o => o.SubOutputs)
            .ThenInclude(so => so.Indicators)
            .ToListAsync();

        foreach (var framework in frameworks)
        {
            foreach (var outcome in framework.Outcomes)
            {
                foreach (var output in outcome.Outputs)
                {
                    foreach (var subOutput in output.SubOutputs)
                    {
                        foreach (var indicator in subOutput.Indicators)
                        {
                            indicator.DisbursementPerformance = project.DisbursementPerformance;
                            indicator.FieldMonitoring = project.FieldMonitoring;
                            indicator.ImpactAssessment = project.ImpactAssessment;
                        }

                        subOutput.DisbursementPerformance = project.DisbursementPerformance;
                        subOutput.FieldMonitoring = project.FieldMonitoring;
                        subOutput.ImpactAssessment = project.ImpactAssessment;
                    }

                    output.DisbursementPerformance = project.DisbursementPerformance;
                    output.FieldMonitoring = project.FieldMonitoring;
                    output.ImpactAssessment = project.ImpactAssessment;
                }

                outcome.DisbursementPerformance = project.DisbursementPerformance;
                outcome.FieldMonitoring = project.FieldMonitoring;
                outcome.ImpactAssessment = project.ImpactAssessment;
            }

            framework.DisbursementPerformance = project.DisbursementPerformance;
            framework.FieldMonitoring = project.FieldMonitoring;
            framework.ImpactAssessment = project.ImpactAssessment;
        }

        await _context.SaveChangesAsync();
    }
}
