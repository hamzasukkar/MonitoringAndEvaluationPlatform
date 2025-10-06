using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Enums;
using MonitoringAndEvaluationPlatform.Models;

public class MonitoringService
{
    private readonly ApplicationDbContext _context;

    public MonitoringService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task UpdateProjectPerformance(int ProjectId)
    {
        var project = await _context.Projects
            .Include(p => p.ProjectIndicators)
                .ThenInclude(pi => pi.Indicator)
                    .ThenInclude(i => i.Measures)
            .FirstOrDefaultAsync(p => p.ProjectID == ProjectId);

        if (project == null)
            return;

        double totalWeightedAchieved = 0;
        double totalWeightedTarget = 0;

        foreach (var projectIndicator in project.ProjectIndicators)
        {
            var indicator = projectIndicator.Indicator;
            double weight = indicator.Weight > 0 ? indicator.Weight : 1;
            
            // Sum all achievements for this indicator
            double achieved = indicator.Measures.Sum(m => m.Value);
            
            // Add to project totals with weighting
            totalWeightedAchieved += achieved * weight;
            totalWeightedTarget += indicator.Target * weight;
        }

        // Calculate aggregate performance: total achievements vs total targets
        project.performance = totalWeightedTarget > 0 
            ? (totalWeightedAchieved / totalWeightedTarget) * 100 
            : 0;

        _context.Projects.Update(project);
        await _context.SaveChangesAsync();
    }


    public async Task UpdateIndicatorPerformance(int indicatorId)
    {
        var indicator = await _context.Indicators
            .Include(i => i.Measures)
            .Include(i => i.ProjectIndicators)
                .ThenInclude(pi => pi.Project)
            .FirstOrDefaultAsync(i => i.IndicatorCode == indicatorId);

        if (indicator == null)
            throw new Exception("Indicator not found");

        // Calculate performance using the static helper method
        indicator.IndicatorsPerformance = CalculateIndicatorPerformanceFromMeasures(indicator);

        _context.Indicators.Update(indicator);
        
        // Update SubOutput performance (existing functionality)
        await UpdateSubOutputPerformance(indicator.SubOutputCode);
        
        // NEW: Update performance for all projects linked to this indicator
        var projectIds = indicator.ProjectIndicators.Select(pi => pi.ProjectId).Distinct().ToList();
        
        foreach (var projectId in projectIds)
        {
            // Update project performance
            await UpdateProjectPerformance(projectId);
            
            // Get the project to update its associated ministries, sectors, and donors
            var project = await _context.Projects
                .Include(p => p.Ministries)
                .Include(p => p.Sectors)
                .Include(p => p.Donors)
                .FirstOrDefaultAsync(p => p.ProjectID == projectId);

            if (project != null)
            {
                // Update ministry performance for each ministry linked to this project
                foreach (var ministry in project.Ministries)
                {
                    await UpdateMinistryPerformanceByMinistryCode(ministry.Code);
                }
                
                // Update sector performance for each sector linked to this project
                foreach (var sector in project.Sectors)
                {
                    await UpdateSectorPerformanceBySectorId(sector.Code);
                }
                
                // Update donor performance for each donor linked to this project
                foreach (var donor in project.Donors)
                {
                    await UpdateDonorPerformanceByDonorCode(donor.Code);
                }
            }
        }
       
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Update disbursement performance for a project based on its DisbursementPerformance activity plans
    /// </summary>
    public async Task UpdateProjectDisbursementPerformance(int projectId)
    {
        var project = await _context.Projects
            .Include(p => p.ActionPlan)
                .ThenInclude(ap => ap.Activities.Where(a => a.ActivityType == ActivityType.DisbursementPerformance))
                    .ThenInclude(a => a.Plans)
            .FirstOrDefaultAsync(p => p.ProjectID == projectId);

        if (project?.ActionPlan == null)
            return;

        // Get all DisbursementPerformance activity plans
        var disbursementActivities = project.ActionPlan.Activities
            .Where(a => a.ActivityType == ActivityType.DisbursementPerformance);

        double totalPlanned = 0;
        double totalRealised = 0;

        foreach (var activity in disbursementActivities)
        {
            foreach (var plan in activity.Plans)
            {
                totalPlanned += plan.Planned;
                totalRealised += plan.Realised;
            }
        }

        // Calculate disbursement performance percentage
        project.DisbursementPerformance = totalPlanned > 0 
            ? (totalRealised / totalPlanned) * 100 
            : 0;

        _context.Projects.Update(project);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Update disbursement performance for an indicator based on all projects linked to it
    /// </summary>
    public async Task UpdateIndicatorDisbursementPerformance(int indicatorCode)
    {
        var indicator = await _context.Indicators
            .Include(i => i.ProjectIndicators)
                .ThenInclude(pi => pi.Project)
                    .ThenInclude(p => p.ActionPlan)
                        .ThenInclude(ap => ap.Activities.Where(a => a.ActivityType == ActivityType.DisbursementPerformance))
                            .ThenInclude(a => a.Plans)
            .FirstOrDefaultAsync(i => i.IndicatorCode == indicatorCode);

        if (indicator == null)
            return;

        double totalPlanned = 0;
        double totalRealised = 0;

        // Aggregate disbursement data from all linked projects
        foreach (var projectIndicator in indicator.ProjectIndicators)
        {
            var project = projectIndicator.Project;
            if (project?.ActionPlan != null)
            {
                var disbursementActivities = project.ActionPlan.Activities
                    .Where(a => a.ActivityType == ActivityType.DisbursementPerformance);

                foreach (var activity in disbursementActivities)
                {
                    foreach (var plan in activity.Plans)
                    {
                        totalPlanned += plan.Planned;
                        totalRealised += plan.Realised;
                    }
                }
            }
        }

        // Calculate indicator disbursement performance
        indicator.DisbursementPerformance = totalPlanned > 0 
            ? (totalRealised / totalPlanned) * 100 
            : 0;

        _context.Indicators.Update(indicator);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Update disbursement performance for ministry based on all its projects
    /// </summary>
    public async Task UpdateMinistryDisbursementPerformance(int ministryCode)
    {
        var ministry = await _context.Ministries
            .Include(m => m.Projects)
                .ThenInclude(p => p.ActionPlan)
                    .ThenInclude(ap => ap.Activities.Where(a => a.ActivityType == ActivityType.DisbursementPerformance))
                        .ThenInclude(a => a.Plans)
            .FirstOrDefaultAsync(m => m.Code == ministryCode);

        if (ministry == null)
            return;

        double totalPlanned = 0;
        double totalRealised = 0;

        // Aggregate disbursement data from all ministry projects
        foreach (var project in ministry.Projects)
        {
            if (project?.ActionPlan != null)
            {
                var disbursementActivities = project.ActionPlan.Activities
                    .Where(a => a.ActivityType == ActivityType.DisbursementPerformance);

                foreach (var activity in disbursementActivities)
                {
                    foreach (var plan in activity.Plans)
                    {
                        totalPlanned += plan.Planned;
                        totalRealised += plan.Realised;
                    }
                }
            }
        }

        // Calculate ministry disbursement performance
        ministry.DisbursementPerformance = totalPlanned > 0 
            ? (totalRealised / totalPlanned) * 100 
            : 0;

        _context.Ministries.Update(ministry);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Update disbursement performance for sector based on all its projects
    /// </summary>
    public async Task UpdateSectorDisbursementPerformance(int sectorCode)
    {
        var sector = await _context.Sectors
            .Include(s => s.Projects)
                .ThenInclude(p => p.ActionPlan)
                    .ThenInclude(ap => ap.Activities.Where(a => a.ActivityType == ActivityType.DisbursementPerformance))
                        .ThenInclude(a => a.Plans)
            .FirstOrDefaultAsync(s => s.Code == sectorCode);

        if (sector == null)
            return;

        double totalPlanned = 0;
        double totalRealised = 0;

        // Aggregate disbursement data from all sector projects
        foreach (var project in sector.Projects)
        {
            if (project?.ActionPlan != null)
            {
                var disbursementActivities = project.ActionPlan.Activities
                    .Where(a => a.ActivityType == ActivityType.DisbursementPerformance);

                foreach (var activity in disbursementActivities)
                {
                    foreach (var plan in activity.Plans)
                    {
                        totalPlanned += plan.Planned;
                        totalRealised += plan.Realised;
                    }
                }
            }
        }

        // Calculate sector disbursement performance
        sector.DisbursementPerformance = totalPlanned > 0 
            ? (totalRealised / totalPlanned) * 100 
            : 0;

        _context.Sectors.Update(sector);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Update disbursement performance for donor based on all its projects
    /// </summary>
    public async Task UpdateDonorDisbursementPerformance(int donorCode)
    {
        var donor = await _context.Donors
            .Include(d => d.Projects)
                .ThenInclude(p => p.ActionPlan)
                    .ThenInclude(ap => ap.Activities.Where(a => a.ActivityType == ActivityType.DisbursementPerformance))
                        .ThenInclude(a => a.Plans)
            .FirstOrDefaultAsync(d => d.Code == donorCode);

        if (donor == null)
            return;

        double totalPlanned = 0;
        double totalRealised = 0;

        // Aggregate disbursement data from all donor projects
        foreach (var project in donor.Projects)
        {
            if (project?.ActionPlan != null)
            {
                var disbursementActivities = project.ActionPlan.Activities
                    .Where(a => a.ActivityType == ActivityType.DisbursementPerformance);

                foreach (var activity in disbursementActivities)
                {
                    foreach (var plan in activity.Plans)
                    {
                        totalPlanned += plan.Planned;
                        totalRealised += plan.Realised;
                    }
                }
            }
        }

        // Calculate donor disbursement performance
        donor.DisbursementPerformance = totalPlanned > 0 
            ? (totalRealised / totalPlanned) * 100 
            : 0;

        _context.Donors.Update(donor);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Update all disbursement performances related to a project when plan data changes
    /// </summary>
    public async Task UpdateDisbursementPerformancesForProject(int projectId)
    {
        // Update project disbursement performance first
        await UpdateProjectDisbursementPerformance(projectId);

        // Get the project with all its relationships
        var project = await _context.Projects
            .Include(p => p.ProjectIndicators)
            .Include(p => p.Ministries)
            .Include(p => p.Sectors)
            .Include(p => p.Donors)
            .FirstOrDefaultAsync(p => p.ProjectID == projectId);

        if (project == null)
            return;

        // Update indicator disbursement performances for all linked indicators
        var indicatorCodes = project.ProjectIndicators.Select(pi => pi.IndicatorCode).Distinct();
        foreach (var indicatorCode in indicatorCodes)
        {
            await UpdateIndicatorDisbursementPerformance(indicatorCode);
        }

        // Update ministry disbursement performances
        foreach (var ministry in project.Ministries)
        {
            await UpdateMinistryDisbursementPerformance(ministry.Code);
        }

        // Update sector disbursement performances
        foreach (var sector in project.Sectors)
        {
            await UpdateSectorDisbursementPerformance(sector.Code);
        }

        // Update donor disbursement performances
        foreach (var donor in project.Donors)
        {
            await UpdateDonorDisbursementPerformance(donor.Code);
        }
    }

    /// <summary>
    /// Calculate performance for an indicator based on its measures against the indicator's target
    /// </summary>
    /// <param name="indicator">The indicator with loaded measures</param>
    /// <returns>Performance percentage (0-100+)</returns>
    public static double CalculateIndicatorPerformanceFromMeasures(Indicator indicator)
    {
        if (indicator?.Measures == null || indicator.Target <= 0)
            return 0;

        // Calculate total achieved from all measures (only Real measures exist now)
        double totalAchieved = indicator.Measures.Sum(m => m.Value);

        // Use the indicator's target as the baseline
        double target = indicator.Target;

        // Calculate performance percentage
        double performance = (totalAchieved / target) * 100;
        
        return Math.Round(performance, 2);
    }

    public async Task UpdateMinistryPerformance(int projectId)
    {
        // 1) Load the project along with its Ministries collection
        var project = await _context.Projects
            .Include(p => p.Ministries)
            .FirstOrDefaultAsync(p => p.ProjectID == projectId);

        if (project == null)
            return; // (Or throw, if you prefer)

        // 2) For each ministry linked to this project, recalc performance:
        foreach (var ministry in project.Ministries)
        {
            double totalWeightedAchieved = 0;
            double totalWeightedTarget = 0;

            // 2a) Find all projects that belong to this same ministry, and include their Measures through indicators
            var ministryProjects = await _context.Projects
                .Where(p => p.Ministries.Any(m => m.Code == ministry.Code))
                .Include(p => p.ProjectIndicators)
                    .ThenInclude(pi => pi.Indicator)
                        .ThenInclude(i => i.Measures)
                .ToListAsync();

            // 2b) Aggregate achievements and targets with proper weighting
            foreach (var ministryProject in ministryProjects)
            {
                foreach (var projectIndicator in ministryProject.ProjectIndicators)
                {
                    var indicator = projectIndicator.Indicator;
                    double weight = indicator.Weight > 0 ? indicator.Weight : 1;
                    
                    double achieved = indicator.Measures.Sum(m => m.Value);
                    
                    totalWeightedAchieved += achieved * weight;
                    totalWeightedTarget += indicator.Target * weight;
                }
            }

            // 2c) Compute aggregate performance percentage
            ministry.IndicatorsPerformance = (totalWeightedTarget > 0)
                ? (totalWeightedAchieved / totalWeightedTarget) * 100
                : 0;

            // EF Core is already tracking 'ministry' because it came in via Include(p => p.Ministries).
        }

        // 3) Persist all ministry changes at once
        await _context.SaveChangesAsync();
    }

    public async Task UpdateSectorPerformance(int projectId)
    {
        // 1) Load the project along with its Sectors collection
        var project = await _context.Projects
            .Include(p => p.Sectors)
            .FirstOrDefaultAsync(p => p.ProjectID == projectId);

        if (project == null)
            return; // (Or throw, if you prefer)

        // 2) For each sector linked to this project, recalc performance:
        foreach (var sector in project.Sectors)
        {
            double totalWeightedAchieved = 0;
            double totalWeightedTarget = 0;

            // 2a) Find all projects that belong to this same sector, and include their Measures through indicators
            var sectorProjects = await _context.Projects
                .Where(p => p.Sectors.Any(s => s.Code == sector.Code))
                .Include(p => p.ProjectIndicators)
                    .ThenInclude(pi => pi.Indicator)
                        .ThenInclude(i => i.Measures)
                .ToListAsync();

            // 2b) Aggregate achievements and targets with proper weighting
            foreach (var sectorProject in sectorProjects)
            {
                foreach (var projectIndicator in sectorProject.ProjectIndicators)
                {
                    var indicator = projectIndicator.Indicator;
                    double weight = indicator.Weight > 0 ? indicator.Weight : 1;
                    
                    double achieved = indicator.Measures.Sum(m => m.Value);
                    
                    totalWeightedAchieved += achieved * weight;
                    totalWeightedTarget += indicator.Target * weight;
                }
            }

            // 2c) Compute aggregate performance percentage
            sector.IndicatorsPerformance = (totalWeightedTarget > 0)
                ? (totalWeightedAchieved / totalWeightedTarget) * 100
                : 0;
        }

        // 3) Persist all sector changes at once
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Updates performance for all donors linked to a specific project
    /// </summary>
    public async Task UpdateDonorPerformance(int projectId)
    {
        // 1) Load the project along with its Donors collection
        var project = await _context.Projects
            .Include(p => p.Donors)
            .FirstOrDefaultAsync(p => p.ProjectID == projectId);

        if (project == null)
            return; // (Or throw, if you prefer)

        // 2) For each donor linked to this project, recalc performance:
        foreach (var donor in project.Donors)
        {
            double totalWeightedAchieved = 0;
            double totalWeightedTarget = 0;

            // 2a) Find all projects that belong to this same donor, and include their Measures through indicators
            var donorProjects = await _context.Projects
                .Where(p => p.Donors.Any(d => d.Code == donor.Code))
                .Include(p => p.ProjectIndicators)
                    .ThenInclude(pi => pi.Indicator)
                        .ThenInclude(i => i.Measures)
                .ToListAsync();

            // 2b) Aggregate achievements and targets with proper weighting
            foreach (var donorProject in donorProjects)
            {
                foreach (var projectIndicator in donorProject.ProjectIndicators)
                {
                    var indicator = projectIndicator.Indicator;
                    double weight = indicator.Weight > 0 ? indicator.Weight : 1;
                    
                    double achieved = indicator.Measures.Sum(m => m.Value);
                    
                    totalWeightedAchieved += achieved * weight;
                    totalWeightedTarget += indicator.Target * weight;
                }
            }

            // 2c) Compute aggregate performance percentage
            donor.IndicatorsPerformance = (totalWeightedTarget > 0)
                ? (totalWeightedAchieved / totalWeightedTarget) * 100
                : 0;

            // EF Core is already tracking 'donor' because it came in via Include(p => p.Donors).
        }

        // 3) Persist all donor changes at once
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Updates donor performance by Donor Code (used in delete operations)
    /// </summary>
    private async Task UpdateDonorPerformanceByDonorCode(int donorCode)
    {
        var donor = await _context.Donors
            .Include(d => d.Projects)
                .ThenInclude(p => p.ProjectIndicators)
                    .ThenInclude(pi => pi.Indicator)
                        .ThenInclude(i => i.Measures)
            .FirstOrDefaultAsync(d => d.Code == donorCode);

        if (donor == null)
            return;

        double totalTarget = 0.0;
        double totalReal = 0.0;

        foreach (var proj in donor.Projects)
        {
            foreach (var projectIndicator in proj.ProjectIndicators)
            {
                var indicator = projectIndicator.Indicator;
                double weight = indicator.Weight > 0 ? indicator.Weight : 1;
                
                double achieved = indicator.Measures.Sum(m => m.Value);
                
                totalReal += achieved * weight;
                totalTarget += indicator.Target * weight;
            }
        }

        donor.IndicatorsPerformance = totalTarget > 0
            ? (totalReal / totalTarget) * 100.0
            : 0.0;

        _context.Donors.Update(donor);
        await _context.SaveChangesAsync();
    }
    // Helper method to calculate weighted average
    private double CalculateWeightedAverage<T>(List<T> items, Func<T, double> valueSelector, Func<T, double> weightSelector)
    {
        var totalWeight = items.Sum(weightSelector);
        return totalWeight > 0 ? items.Sum(i => valueSelector(i) * weightSelector(i)) / totalWeight : 0;
    }

    private async Task UpdateSubOutputPerformance(int subOutputCode)
    {
        var subOutput = await _context.SubOutputs.FirstOrDefaultAsync(i => i.Code == subOutputCode);
        if (subOutput == null) return;

        var indicators = await _context.Indicators
            .Where(i => i.SubOutputCode == subOutput.Code)
            .ToListAsync();

        // Calculate weighted performance of SubOutput based on its Indicators
        subOutput.IndicatorsPerformance = CalculateWeightedAverage(indicators, i => i.IndicatorsPerformance, i => i.Weight);

        await _context.SaveChangesAsync();

        await UpdateOutputPerformance(subOutput.OutputCode);
    }

    private async Task UpdateOutputPerformance(int outputCode)
    {
        var output = await _context.Outputs.FirstOrDefaultAsync(i => i.Code == outputCode);
        if (output == null) return;

        var subOutputs = await _context.SubOutputs
            .Where(s => s.OutputCode == output.Code)
            .ToListAsync();

        // Calculate weighted performance of Output based on its SubOutputs
        output.IndicatorsPerformance = CalculateWeightedAverage(subOutputs, s => s.IndicatorsPerformance, s => s.Weight);

        await _context.SaveChangesAsync();

        await UpdateOutcomePerformance(output.OutcomeCode);
    }

    private async Task UpdateOutcomePerformance(int outcomeCode)
    {
        var outcome = await _context.Outcomes.FirstOrDefaultAsync(i => i.Code == outcomeCode);
        if (outcome == null) return;

        var outputs = await _context.Outputs
            .Where(o => o.OutcomeCode == outcome.Code)
            .ToListAsync();

        // Calculate weighted performance of Outcome based on its Outputs
        outcome.IndicatorsPerformance = CalculateWeightedAverage(outputs, o => o.IndicatorsPerformance, o => o.Weight);

        await _context.SaveChangesAsync();

        await UpdateFrameworkPerformance(outcome.FrameworkCode);
    }

    private async Task UpdateFrameworkPerformance(int frameworkCode)
    {
        var framework = await _context.Frameworks.FirstOrDefaultAsync(i => i.Code == frameworkCode);
        if (framework == null) return;

        var outcomes = await _context.Outcomes
            .Where(o => o.FrameworkCode == framework.Code)
            .ToListAsync();

        // Calculate weighted performance of Framework based on its Outcomes
        framework.IndicatorsPerformance = CalculateWeightedAverage(outcomes, o => o.IndicatorsPerformance, o => o.Weight);

        await _context.SaveChangesAsync();
    }

    private double CalculateAveragePerformance(List<double> performances)
    {
        return performances.Any() ? performances.Sum() / performances.Count : 0;
    }


    public async Task AddMeasureToIndicator(int indicatorId, double value)
    {
        var measure = new Measure
        {
            IndicatorCode = indicatorId,
            Value = value,
            Date = DateTime.UtcNow
        };

        _context.Measures.Add(measure);
        await _context.SaveChangesAsync();

        // Update indicator performance and cascade up the hierarchy
        await UpdateIndicatorPerformance(indicatorId);
    }

    public async Task DeleteMeasureAndRecalculateAsync(int measureCode)
    {
        var measure = await _context.Measures
            .Include(m => m.Indicator)
                .ThenInclude(i => i.ProjectIndicators)
                    .ThenInclude(pi => pi.Project)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Code == measureCode);

        if (measure == null)
            throw new InvalidOperationException($"Measure with code {measureCode} not found.");

        var indicatorId = measure.IndicatorCode;
        // Get all projects related to this measure through indicators
        var projectIds = measure.Indicator.ProjectIndicators
            .Select(pi => pi.ProjectId)
            .Distinct()
            .ToList();

        _context.Measures.Remove(measure);
        await _context.SaveChangesAsync();

        await UpdateIndicatorPerformance(indicatorId);
        
        // Update performance for all related projects
        foreach (var projectId in projectIds)
        {
            await UpdateProjectPerformance(projectId);
            await UpdateMinistryPerformance(projectId);
            await UpdateSectorPerformance(projectId);
            await UpdateDonorPerformance(projectId);
        }
    }

    /// <summary>
    /// Deletes a Project (cascade removes measures) and recalculates all related performance metrics.
    /// </summary>
    public async Task DeleteProjectAndRecalculateAsync(int projectId)
    {
        // 1. Load project with related indicators and ministries
        var project = await _context.Projects
            .Include(p => p.ProjectIndicators)
                .ThenInclude(pi => pi.Indicator)
                    .ThenInclude(i => i.Measures)
            .Include(p => p.Ministries)
            .Include(p => p.Sectors)
            .Include(p => p.Donors)
            .FirstOrDefaultAsync(p => p.ProjectID == projectId);

        if (project == null)
            throw new InvalidOperationException($"Project with ID {projectId} not found.");

        // 2. Capture related IDs
        var indicatorIds = project.ProjectIndicators.Select(pi => pi.IndicatorCode).Distinct().ToList();
        var ministryCodes = project.Ministries.Select(m => m.Code).Distinct().ToList();
        var sectorIds = project.Sectors.Select(s => s.Code).Distinct().ToList();
        var donorCodes = project.Donors.Select(d => d.Code).Distinct().ToList();

        // 3. Remove the project (cascade deletes measures)
        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();

        // 4. Recalculate Indicator and hierarchical metrics
        foreach (var indId in indicatorIds)
        {
            await UpdateIndicatorPerformance(indId);
        }

        // 5. Recalculate ministry performance for affected ministries
        foreach (var ministryCode in ministryCodes)
        {
            await UpdateMinistryPerformanceByMinistryCode(ministryCode);
        }

        // 6. Recalculate sector performance for affected sectors
        foreach (var sectorId in sectorIds)
        {
            await UpdateSectorPerformanceBySectorId(sectorId);
        }
        // 7. Recalculate donor performance for affected donors ✅ Add this block
        foreach (var donorCode in donorCodes)
        {
            await UpdateDonorPerformanceByDonorCode(donorCode);
        }
    }

    /// <summary>
    /// Always recalc ministry performance by Ministry Code.
    /// </summary>
    private async Task UpdateMinistryPerformanceByMinistryCode(int ministryCode)
    {
        var ministry = await _context.Ministries
            .Include(m => m.Projects)
                .ThenInclude(p => p.ProjectIndicators)
                    .ThenInclude(pi => pi.Indicator)
                        .ThenInclude(i => i.Measures)
            .FirstOrDefaultAsync(m => m.Code == ministryCode);

        if (ministry == null)
            return;

        double totalTarget = 0.0;
        double totalReal = 0.0;

        foreach (var proj in ministry.Projects)
        {
            foreach (var projectIndicator in proj.ProjectIndicators)
            {
                var indicator = projectIndicator.Indicator;
                double weight = indicator.Weight > 0 ? indicator.Weight : 1;
                
                double achieved = indicator.Measures.Sum(m => m.Value);
                
                totalReal += achieved * weight;
                totalTarget += indicator.Target * weight;
            }
        }

        ministry.IndicatorsPerformance = totalTarget > 0
            ? (totalReal / totalTarget) * 100.0
            : 0.0;

        _context.Ministries.Update(ministry);
        await _context.SaveChangesAsync();
    }

    private async Task UpdateSectorPerformanceBySectorId(int sectorId)
    {
        var sector = await _context.Sectors
            .Include(s => s.Projects)
                .ThenInclude(p => p.ProjectIndicators)
                    .ThenInclude(pi => pi.Indicator)
                        .ThenInclude(i => i.Measures)
            .FirstOrDefaultAsync(s => s.Code == sectorId);

        if (sector == null)
            return;

        double totalTarget = 0.0;
        double totalReal = 0.0;

        foreach (var proj in sector.Projects)
        {
            foreach (var projectIndicator in proj.ProjectIndicators)
            {
                var indicator = projectIndicator.Indicator;
                double weight = indicator.Weight > 0 ? indicator.Weight : 1;
                
                double achieved = indicator.Measures.Sum(m => m.Value);
                
                totalReal += achieved * weight;
                totalTarget += indicator.Target * weight;
            }
        }

        sector.IndicatorsPerformance = totalTarget > 0
            ? (totalReal / totalTarget) * 100.0
            : 0.0;

        _context.Sectors.Update(sector);
        await _context.SaveChangesAsync();
    }



}