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
   
    public async Task RecalculatePerformanceAfterProjectDeletion(Project deletedProject)
    {
        // Get all indicators that were affected by the deleted project through measures
        var affectedIndicatorIds = await _context.Measures
            .Where(m => m.ProjectID == deletedProject.ProjectID)
            .Select(m => m.IndicatorCode)
            .Distinct()
            .ToListAsync();

        if (!affectedIndicatorIds.Any())
            return;

        // Recalculate performance for affected indicators (excluding the deleted project)
        await RecalculateIndicatorsPerformance(affectedIndicatorIds);
    }

    public async Task RecalculatePerformanceAfterIndicatorDeletion(Indicator deletedIndicator)
    {
        // Get the SubOutput that contained the deleted indicator
        var subOutputCode = deletedIndicator.SubOutputCode;
        
        if (subOutputCode == 0)
            return;

        // Recalculate performance for the affected SubOutput and cascade upward
        await RecalculateParentEntitiesPerformance(subOutputCode);
    }

    private async Task RecalculateIndicatorsPerformance(List<int> indicatorIds)
    {
        // B. Calculate DisbursementPerformance for each affected Indicator
        var indicatorsToUpdate = await _context.Indicators
            .Where(i => indicatorIds.Contains(i.IndicatorCode))
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
            else
            {
                // No projects linked, reset to 0
                indicator.DisbursementPerformance = 0;
                indicator.FieldMonitoring = 0;
                indicator.ImpactAssessment = 0;
            }

            // ✅ Mark entity as modified
            _context.Indicators.Update(indicator);
        }

        // C. Calculate DisbursementPerformance for SubOutputs
        var affectedSubOutputIds = indicatorsToUpdate.Select(i => i.SubOutputCode).Distinct().Where(id => id != 0).ToList();
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
            else
            {
                subOutput.DisbursementPerformance = 0;
                subOutput.FieldMonitoring = 0;
                subOutput.ImpactAssessment = 0;
            }

            // ✅ Mark entity as modified
            _context.SubOutputs.Update(subOutput);
        }

        // D. Calculate DisbursementPerformance for Outputs
        var affectedOutputIds = subOutputsToUpdate.Select(so => so.OutputCode).Distinct().Where(id => id != 0).ToList();
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
            else
            {
                output.DisbursementPerformance = 0;
                output.FieldMonitoring = 0;
                output.ImpactAssessment = 0;
            }

            // ✅ Mark entity as modified
            _context.Outputs.Update(output);
        }

        // E. Calculate DisbursementPerformance for Outcomes
        var affectedOutcomeIds = outputsToUpdate.Select(o => o.OutcomeCode).Distinct().Where(id => id != 0).ToList();
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
            else
            {
                outcome.DisbursementPerformance = 0;
                outcome.FieldMonitoring = 0;
                outcome.ImpactAssessment = 0;
            }

            // ✅ Mark entity as modified
            _context.Outcomes.Update(outcome);
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
            else
            {
                framework.DisbursementPerformance = 0;
                framework.FieldMonitoring = 0;
                framework.ImpactAssessment = 0;
            }

            // ✅ Mark entity as modified
            _context.Frameworks.Update(framework);
        }

        // G. Save all changes
        await _context.SaveChangesAsync();
    }
    private async Task RecalculateParentEntitiesPerformance(int subOutputCode)
    {
        // A. Recalculate SubOutput performance from remaining indicators
        var subOutputToUpdate = await _context.SubOutputs
            .Where(so => so.Code == subOutputCode)
            .Include(so => so.Indicators)
            .FirstOrDefaultAsync();

        if (subOutputToUpdate != null)
        {
            if (subOutputToUpdate.Indicators.Any())
            {
                subOutputToUpdate.DisbursementPerformance = (int)subOutputToUpdate.Indicators.Average(i => i.DisbursementPerformance);
                subOutputToUpdate.FieldMonitoring = (int)subOutputToUpdate.Indicators.Average(i => i.FieldMonitoring);
                subOutputToUpdate.ImpactAssessment = (int)subOutputToUpdate.Indicators.Average(i => i.ImpactAssessment);
            }
            else
            {
                // No indicators left, reset to 0
                subOutputToUpdate.DisbursementPerformance = 0;
                subOutputToUpdate.FieldMonitoring = 0;
                subOutputToUpdate.ImpactAssessment = 0;
            }

            // B. Recalculate Output performance from remaining SubOutputs
            var outputCode = subOutputToUpdate.OutputCode;
            if (outputCode != 0)
            {
                var outputToUpdate = await _context.Outputs
                    .Where(o => o.Code == outputCode)
                    .Include(o => o.SubOutputs)
                    .FirstOrDefaultAsync();

                if (outputToUpdate != null)
                {
                    if (outputToUpdate.SubOutputs.Any())
                    {
                        outputToUpdate.DisbursementPerformance = (int)outputToUpdate.SubOutputs.Average(so => so.DisbursementPerformance);
                        outputToUpdate.FieldMonitoring = (int)outputToUpdate.SubOutputs.Average(so => so.FieldMonitoring);
                        outputToUpdate.ImpactAssessment = (int)outputToUpdate.SubOutputs.Average(so => so.ImpactAssessment);
                    }
                    else
                    {
                        outputToUpdate.DisbursementPerformance = 0;
                        outputToUpdate.FieldMonitoring = 0;
                        outputToUpdate.ImpactAssessment = 0;
                    }

                    // C. Recalculate Outcome performance from remaining Outputs
                    var outcomeCode = outputToUpdate.OutcomeCode;
                    if (outcomeCode != 0)
                    {
                        var outcomeToUpdate = await _context.Outcomes
                            .Where(oc => oc.Code == outcomeCode)
                            .Include(oc => oc.Outputs)
                            .FirstOrDefaultAsync();

                        if (outcomeToUpdate != null)
                        {
                            if (outcomeToUpdate.Outputs.Any())
                            {
                                outcomeToUpdate.DisbursementPerformance = (int)outcomeToUpdate.Outputs.Average(o => o.DisbursementPerformance);
                                outcomeToUpdate.FieldMonitoring = (int)outcomeToUpdate.Outputs.Average(o => o.FieldMonitoring);
                                outcomeToUpdate.ImpactAssessment = (int)outcomeToUpdate.Outputs.Average(o => o.ImpactAssessment);
                            }
                            else
                            {
                                outcomeToUpdate.DisbursementPerformance = 0;
                                outcomeToUpdate.FieldMonitoring = 0;
                                outcomeToUpdate.ImpactAssessment = 0;
                            }

                            // D. Recalculate Framework performance from remaining Outcomes
                            var frameworkCode = outcomeToUpdate.FrameworkCode;
                            if (frameworkCode != 0)
                            {
                                var frameworkToUpdate = await _context.Frameworks
                                    .Where(f => f.Code == frameworkCode)
                                    .Include(f => f.Outcomes)
                                    .FirstOrDefaultAsync();

                                if (frameworkToUpdate != null)
                                {
                                    if (frameworkToUpdate.Outcomes.Any())
                                    {
                                        frameworkToUpdate.DisbursementPerformance = (int)frameworkToUpdate.Outcomes.Average(oc => oc.DisbursementPerformance);
                                        frameworkToUpdate.FieldMonitoring = (int)frameworkToUpdate.Outcomes.Average(oc => oc.FieldMonitoring);
                                        frameworkToUpdate.ImpactAssessment = (int)frameworkToUpdate.Outcomes.Average(oc => oc.ImpactAssessment);
                                    }
                                    else
                                    {
                                        frameworkToUpdate.DisbursementPerformance = 0;
                                        frameworkToUpdate.FieldMonitoring = 0;
                                        frameworkToUpdate.ImpactAssessment = 0;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // E. Save all changes
        await _context.SaveChangesAsync();
    }
}
