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
        
        // Update disbursement performance across all levels
        var monitoringService = new MonitoringService(_context);
        await monitoringService.UpdateDisbursementPerformancesForProject(existingPlan.Activity.ActionPlan.Project.ProjectID);
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

        // Update Framework, Outcomes, Outputs, SubOutputs, Indicators, Donors, Sectors, Ministries
        // This will save all changes in a single transaction
        await UpdateRelatedEntities(project);
    }


    private async Task UpdateRelatedEntities(Project project)
    {
        // This method recalculates performance for the entire hierarchy and cross-cutting entities
        // Flow: Project → Indicators → SubOutputs → Outputs → Outcomes → Frameworks + Donors/Sectors/Ministries
        
        // A. Get all affected indicators through project indicators
        var affectedIndicatorIds = await _context.ProjectIndicators
            .Where(pi => pi.ProjectId == project.ProjectID)
            .Select(pi => pi.IndicatorCode)
            .Distinct()
            .ToListAsync();

        // B. Calculate DisbursementPerformance for each affected Indicator
        var indicatorsToUpdate = await _context.Indicators
            .Where(i => affectedIndicatorIds.Contains(i.IndicatorCode))
            .Include(i => i.Measures)
            .Include(i => i.ProjectIndicators)
                .ThenInclude(pi => pi.Project)
            .ToListAsync();

        foreach (var indicator in indicatorsToUpdate)
        {
            // Calculate average DisbursementPerformance from all projects linked to this indicator via ProjectIndicators
            var linkedProjects = indicator.ProjectIndicators.Select(pi => pi.Project).Where(p => p != null).ToList();
            if (linkedProjects.Any())
            {
                indicator.DisbursementPerformance = (int)linkedProjects.Average(p => p.DisbursementPerformance);
                indicator.FieldMonitoring = (int)linkedProjects.Average(p => p.FieldMonitoring);
                indicator.ImpactAssessment = (int)linkedProjects.Average(p => p.ImpactAssessment);
            }
            else
            {
                indicator.DisbursementPerformance = 0;
                indicator.FieldMonitoring = 0;
                indicator.ImpactAssessment = 0;
            }
            _context.Indicators.Update(indicator);
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
            else
            {
                subOutput.DisbursementPerformance = 0;
                subOutput.FieldMonitoring = 0;
                subOutput.ImpactAssessment = 0;
            }
            _context.SubOutputs.Update(subOutput);
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
            else
            {
                output.DisbursementPerformance = 0;
                output.FieldMonitoring = 0;
                output.ImpactAssessment = 0;
            }
            _context.Outputs.Update(output);
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
            else
            {
                outcome.DisbursementPerformance = 0;
                outcome.FieldMonitoring = 0;
                outcome.ImpactAssessment = 0;
            }
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
            _context.Frameworks.Update(framework);
        }

        // H. Calculate DisbursementPerformance for Donors (cross-cutting entity)
        // Get all donors that contain the current project, then recalculate their performance
        var donorsToUpdate = await _context.Donors
            .Include(d => d.Projects)
            .Where(d => d.Projects.Any(p => p.ProjectID == project.ProjectID))
            .ToListAsync();

        foreach (var donor in donorsToUpdate)
        {
            if (donor.Projects.Any())
            {
                donor.DisbursementPerformance = (int)donor.Projects.Average(p => p.DisbursementPerformance);
                donor.FieldMonitoring = (int)donor.Projects.Average(p => p.FieldMonitoring);
                donor.ImpactAssessment = (int)donor.Projects.Average(p => p.ImpactAssessment);
            }
            else
            {
                donor.DisbursementPerformance = 0;
                donor.FieldMonitoring = 0;
                donor.ImpactAssessment = 0;
            }
            _context.Donors.Update(donor);
        }

        // I. Calculate DisbursementPerformance for Sectors (cross-cutting entity)
        // Get all sectors that contain the current project, then recalculate their performance
        var sectorsToUpdate = await _context.Sectors
            .Include(s => s.Projects)
            .Where(s => s.Projects.Any(p => p.ProjectID == project.ProjectID))
            .ToListAsync();

        foreach (var sector in sectorsToUpdate)
        {
            if (sector.Projects.Any())
            {
                sector.DisbursementPerformance = (int)sector.Projects.Average(p => p.DisbursementPerformance);
                sector.FieldMonitoring = (int)sector.Projects.Average(p => p.FieldMonitoring);
                sector.ImpactAssessment = (int)sector.Projects.Average(p => p.ImpactAssessment);
            }
            else
            {
                sector.DisbursementPerformance = 0;
                sector.FieldMonitoring = 0;
                sector.ImpactAssessment = 0;
            }
            _context.Sectors.Update(sector);
        }

        // J. Calculate DisbursementPerformance for Ministries (cross-cutting entity)
        // Get all ministries that contain the current project, then recalculate their performance
        var ministriesToUpdate = await _context.Ministries
            .Include(m => m.Projects)
            .Where(m => m.Projects.Any(p => p.ProjectID == project.ProjectID))
            .ToListAsync();

        foreach (var ministry in ministriesToUpdate)
        {
            if (ministry.Projects.Any())
            {
                ministry.DisbursementPerformance = (int)ministry.Projects.Average(p => p.DisbursementPerformance);
                ministry.FieldMonitoring = (int)ministry.Projects.Average(p => p.FieldMonitoring);
                ministry.ImpactAssessment = (int)ministry.Projects.Average(p => p.ImpactAssessment);
            }
            else
            {
                ministry.DisbursementPerformance = 0;
                ministry.FieldMonitoring = 0;
                ministry.ImpactAssessment = 0;
            }
            _context.Ministries.Update(ministry);
        }

        // K. Save all changes
        await _context.SaveChangesAsync();
    }
   
    public async Task RecalculatePerformanceAfterProjectDeletion(Project deletedProject)
    {
        // Get all indicators that were affected by the deleted project through project indicators
        var affectedIndicatorIds = await _context.ProjectIndicators
            .Where(pi => pi.ProjectId == deletedProject.ProjectID)
            .Select(pi => pi.IndicatorCode)
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
            .Include(i => i.ProjectIndicators)
                .ThenInclude(pi => pi.Project)
            .ToListAsync();

        foreach (var indicator in indicatorsToUpdate)
        {
            // Calculate average DisbursementPerformance from all projects linked to this indicator via ProjectIndicators
            var linkedProjects = indicator.ProjectIndicators.Select(pi => pi.Project).Where(p => p != null).ToList();
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

        // H. Calculate DisbursementPerformance for Donors (cross-cutting entity)
        // After project deletion, recalculate all donors to ensure accurate performance
        var donorsToUpdate = await _context.Donors
            .Include(d => d.Projects)
            .ToListAsync();

        foreach (var donor in donorsToUpdate)
        {
            if (donor.Projects.Any())
            {
                donor.DisbursementPerformance = (int)donor.Projects.Average(p => p.DisbursementPerformance);
                donor.FieldMonitoring = (int)donor.Projects.Average(p => p.FieldMonitoring);
                donor.ImpactAssessment = (int)donor.Projects.Average(p => p.ImpactAssessment);
            }
            else
            {
                donor.DisbursementPerformance = 0;
                donor.FieldMonitoring = 0;
                donor.ImpactAssessment = 0;
            }
            _context.Donors.Update(donor);
        }

        // I. Calculate DisbursementPerformance for Sectors (cross-cutting entity)
        // After project deletion, recalculate all sectors to ensure accurate performance
        var sectorsToUpdate = await _context.Sectors
            .Include(s => s.Projects)
            .ToListAsync();

        foreach (var sector in sectorsToUpdate)
        {
            if (sector.Projects.Any())
            {
                sector.DisbursementPerformance = (int)sector.Projects.Average(p => p.DisbursementPerformance);
                sector.FieldMonitoring = (int)sector.Projects.Average(p => p.FieldMonitoring);
                sector.ImpactAssessment = (int)sector.Projects.Average(p => p.ImpactAssessment);
            }
            else
            {
                sector.DisbursementPerformance = 0;
                sector.FieldMonitoring = 0;
                sector.ImpactAssessment = 0;
            }
            _context.Sectors.Update(sector);
        }

        // J. Calculate DisbursementPerformance for Ministries (cross-cutting entity)
        // After project deletion, recalculate all ministries to ensure accurate performance
        var ministriesToUpdate = await _context.Ministries
            .Include(m => m.Projects)
            .ToListAsync();

        foreach (var ministry in ministriesToUpdate)
        {
            if (ministry.Projects.Any())
            {
                ministry.DisbursementPerformance = (int)ministry.Projects.Average(p => p.DisbursementPerformance);
                ministry.FieldMonitoring = (int)ministry.Projects.Average(p => p.FieldMonitoring);
                ministry.ImpactAssessment = (int)ministry.Projects.Average(p => p.ImpactAssessment);
            }
            else
            {
                ministry.DisbursementPerformance = 0;
                ministry.FieldMonitoring = 0;
                ministry.ImpactAssessment = 0;
            }
            _context.Ministries.Update(ministry);
        }

        // K. Save all changes
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

        // F. Recalculate Donors, Sectors, Ministries performance (get projects from the deleted indicator's subOutput)
        if (subOutputToUpdate != null)
        {
            // Get all projects that are linked to indicators in the same subOutput via ProjectIndicators
            var projectsInSubOutput = await _context.ProjectIndicators
                .Where(pi => subOutputToUpdate.Indicators.Select(i => i.IndicatorCode).Contains(pi.IndicatorCode))
                .Select(pi => pi.ProjectId)
                .Distinct()
                .ToListAsync();

            // H. Calculate DisbursementPerformance for Donors (cross-cutting entity)
            var donorsToUpdate = await _context.Donors
                .Where(d => d.Projects.Any(p => projectsInSubOutput.Contains(p.ProjectID)))
                .Include(d => d.Projects)
                .ToListAsync();

            foreach (var donor in donorsToUpdate)
            {
                if (donor.Projects.Any())
                {
                    donor.DisbursementPerformance = (int)donor.Projects.Average(p => p.DisbursementPerformance);
                    donor.FieldMonitoring = (int)donor.Projects.Average(p => p.FieldMonitoring);
                    donor.ImpactAssessment = (int)donor.Projects.Average(p => p.ImpactAssessment);
                }
                else
                {
                    donor.DisbursementPerformance = 0;
                    donor.FieldMonitoring = 0;
                    donor.ImpactAssessment = 0;
                }
                _context.Donors.Update(donor);
            }

            // I. Calculate DisbursementPerformance for Sectors (cross-cutting entity)
            var sectorsToUpdate = await _context.Sectors
                .Where(s => s.Projects.Any(p => projectsInSubOutput.Contains(p.ProjectID)))
                .Include(s => s.Projects)
                .ToListAsync();

            foreach (var sector in sectorsToUpdate)
            {
                if (sector.Projects.Any())
                {
                    sector.DisbursementPerformance = (int)sector.Projects.Average(p => p.DisbursementPerformance);
                    sector.FieldMonitoring = (int)sector.Projects.Average(p => p.FieldMonitoring);
                    sector.ImpactAssessment = (int)sector.Projects.Average(p => p.ImpactAssessment);
                }
                else
                {
                    sector.DisbursementPerformance = 0;
                    sector.FieldMonitoring = 0;
                    sector.ImpactAssessment = 0;
                }
                _context.Sectors.Update(sector);
            }

            // J. Calculate DisbursementPerformance for Ministries (cross-cutting entity)
            var ministriesToUpdate = await _context.Ministries
                .Where(m => m.Projects.Any(p => projectsInSubOutput.Contains(p.ProjectID)))
                .Include(m => m.Projects)
                .ToListAsync();

            foreach (var ministry in ministriesToUpdate)
            {
                if (ministry.Projects.Any())
                {
                    ministry.DisbursementPerformance = (int)ministry.Projects.Average(p => p.DisbursementPerformance);
                    ministry.FieldMonitoring = (int)ministry.Projects.Average(p => p.FieldMonitoring);
                    ministry.ImpactAssessment = (int)ministry.Projects.Average(p => p.ImpactAssessment);
                }
                else
                {
                    ministry.DisbursementPerformance = 0;
                    ministry.FieldMonitoring = 0;
                    ministry.ImpactAssessment = 0;
                }
                _context.Ministries.Update(ministry);
            }
        }

        // K. Save all changes
        await _context.SaveChangesAsync();
    }

    public async Task UpdateRelatedEntitiesAfterProjectEdit(Project project)
    {
        // When a project is edited (not performance-related changes), we only need to recalculate
        // Donors, Sectors, and Ministries that contain this project to ensure accurate averages
        
        // H. Calculate DisbursementPerformance for Donors (cross-cutting entity)
        var donorsToUpdate = await _context.Donors
            .Include(d => d.Projects)
            .Where(d => d.Projects.Any(p => p.ProjectID == project.ProjectID))
            .ToListAsync();

        foreach (var donor in donorsToUpdate)
        {
            if (donor.Projects.Any())
            {
                donor.DisbursementPerformance = (int)donor.Projects.Average(p => p.DisbursementPerformance);
                donor.FieldMonitoring = (int)donor.Projects.Average(p => p.FieldMonitoring);
                donor.ImpactAssessment = (int)donor.Projects.Average(p => p.ImpactAssessment);
            }
            else
            {
                donor.DisbursementPerformance = 0;
                donor.FieldMonitoring = 0;
                donor.ImpactAssessment = 0;
            }
            _context.Donors.Update(donor);
        }

        // I. Calculate DisbursementPerformance for Sectors (cross-cutting entity)
        var sectorsToUpdate = await _context.Sectors
            .Include(s => s.Projects)
            .Where(s => s.Projects.Any(p => p.ProjectID == project.ProjectID))
            .ToListAsync();

        foreach (var sector in sectorsToUpdate)
        {
            if (sector.Projects.Any())
            {
                sector.DisbursementPerformance = (int)sector.Projects.Average(p => p.DisbursementPerformance);
                sector.FieldMonitoring = (int)sector.Projects.Average(p => p.FieldMonitoring);
                sector.ImpactAssessment = (int)sector.Projects.Average(p => p.ImpactAssessment);
            }
            else
            {
                sector.DisbursementPerformance = 0;
                sector.FieldMonitoring = 0;
                sector.ImpactAssessment = 0;
            }
            _context.Sectors.Update(sector);
        }

        // J. Calculate DisbursementPerformance for Ministries (cross-cutting entity)
        var ministriesToUpdate = await _context.Ministries
            .Include(m => m.Projects)
            .Where(m => m.Projects.Any(p => p.ProjectID == project.ProjectID))
            .ToListAsync();

        foreach (var ministry in ministriesToUpdate)
        {
            if (ministry.Projects.Any())
            {
                ministry.DisbursementPerformance = (int)ministry.Projects.Average(p => p.DisbursementPerformance);
                ministry.FieldMonitoring = (int)ministry.Projects.Average(p => p.FieldMonitoring);
                ministry.ImpactAssessment = (int)ministry.Projects.Average(p => p.ImpactAssessment);
            }
            else
            {
                ministry.DisbursementPerformance = 0;
                ministry.FieldMonitoring = 0;
                ministry.ImpactAssessment = 0;
            }
            _context.Ministries.Update(ministry);
        }

        // Save all changes
        await _context.SaveChangesAsync();
    }
}
