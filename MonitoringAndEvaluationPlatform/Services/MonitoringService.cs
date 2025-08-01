﻿using Microsoft.CodeAnalysis.CSharp;
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
            .Include(p => p.Measures)
            .Include(p => p.logicalFramework)
            .FirstOrDefaultAsync(p => p.ProjectID == ProjectId);

        if (project == null)
            return;

        // Calculate Measures Performance
        double totalAchieved = project.Measures
            .Where(m => m.ValueType == MeasureValueType.Real)
            .Sum(m => m.Value);

        double target = project.Measures
            .Where(m => m.ValueType == MeasureValueType.Target)
            .Sum(m => m.Value);

        double measuresPerformance = (target > 0) ? (totalAchieved / target) * 100 : 0;

        // Calculate LogicalFrameworks Average Performance
        double logicalFrameworksPerformance = 0;
        if (project.logicalFramework != null && project.logicalFramework.Any())
        {
            logicalFrameworksPerformance = project.logicalFramework
                .Average(lf => lf.Performance); // Assuming LogicalFramework has a 'Performance' property (0-100 %)
        }

        // Combine the two performances
        if (logicalFrameworksPerformance > 0 && measuresPerformance > 0)
        {
            project.performance = (measuresPerformance + logicalFrameworksPerformance) / 2;
        }
        else if (logicalFrameworksPerformance > 0)
        {
            project.performance = logicalFrameworksPerformance;
        }
        else
        {
            project.performance = measuresPerformance;
        }

        _context.Projects.Update(project);
        await _context.SaveChangesAsync();
    }


    public async Task UpdateIndicatorPerformance(int indicatorId)
    {
        var indicator = await _context.Indicators
            .Include(i => i.Measures)
            .FirstOrDefaultAsync(i => i.IndicatorCode == indicatorId);

        if (indicator == null)
            throw new Exception("Indicator not found");

        double totalAchieved = indicator.Measures
            .Where(m => m.ValueType == MeasureValueType.Real)
            .Sum(m => m.Value); // Sum of all real measure values

        double target = indicator.Target;

        indicator.IndicatorsPerformance = (target > 0) ? (totalAchieved / target) * 100 : 0;

        _context.Indicators.Update(indicator);
        await UpdateSubOutputPerformance(indicator.SubOutputCode);
       
        await _context.SaveChangesAsync();
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
            double totalMinistryTarget = 0;
            double totalMinistryReal = 0;

            // 2a) Find all projects that belong to this same ministry, and include their Measures
            var ministryProjects = await _context.Projects
                .Where(p => p.Ministries.Any(m => m.Code == ministry.Code))
                .Include(p => p.Measures)
                .ToListAsync();

            // 2b) Sum up Target/Real values from each Measure on those projects
            foreach (var ministryProject in ministryProjects)
            {
                foreach (var measure in ministryProject.Measures)
                {
                    if (measure.ValueType == MeasureValueType.Real)
                        totalMinistryReal += measure.Value;
                    else if (measure.ValueType == MeasureValueType.Target)
                        totalMinistryTarget += measure.Value;
                }
            }

            // 2c) Compute performance percentage (or zero if no targets)
            ministry.IndicatorsPerformance = (totalMinistryTarget > 0)
                ? (totalMinistryReal / totalMinistryTarget) * 100
                : 0;

            // EF Core is already tracking 'ministry' because it came in via Include(p => p.Ministries).
            // No need to call _context.Ministries.Update(ministry) explicitly.
        }

        // 3) Persist all ministry changes at once
        await _context.SaveChangesAsync();
    }


    private async Task UpdateOutputPerformance(int outputCode)
    {
        var output = await _context.Outputs.FirstOrDefaultAsync(i => i.Code == outputCode);

        if (output == null) return;

        var subOutputs = await _context.SubOutputs.Where(i => i.OutputCode == output.Code).ToListAsync();
        output.IndicatorsPerformance = CalculateAveragePerformance(subOutputs.Select(s => s.IndicatorsPerformance).ToList());

        await _context.SaveChangesAsync();

        await UpdateOutcomePerformance(output.OutcomeCode);
    }

    private async Task UpdateOutcomePerformance(int outcomeCode)
    {
        var outcome = await _context.Outcomes.FirstOrDefaultAsync(i => i.Code == outcomeCode);

        if (outcome == null) return;

        var outputs = await _context.Outputs.Where(i => i.OutcomeCode == outcome.Code).ToListAsync();
        outcome.IndicatorsPerformance = CalculateAveragePerformance(outputs.Select(o => o.IndicatorsPerformance).ToList());

        await _context.SaveChangesAsync();

        await UpdateFrameworkPerformance(outcome.FrameworkCode);
    }

    private async Task UpdateFrameworkPerformance(int frameworkCode)
    {
        var framework = await _context.Frameworks.FirstOrDefaultAsync(i => i.Code == frameworkCode);

        if (framework == null) return;

        var outcomes = await _context.Outcomes.Where(i => i.FrameworkCode == framework.Code).ToListAsync();
        framework.IndicatorsPerformance = CalculateAveragePerformance(outcomes.Select(o => o.IndicatorsPerformance).ToList());

        await _context.SaveChangesAsync();
    }

    private async Task UpdateSubOutputPerformance(int subOutputCode)
    {
        var subOutput = await _context.SubOutputs.FirstOrDefaultAsync(i => i.Code == subOutputCode);

        if (subOutput == null) return;

        var indicators = await _context.Indicators.Where(i => i.SubOutputCode == subOutput.Code).ToListAsync();
        subOutput.IndicatorsPerformance = CalculateAveragePerformance(indicators.Select(i => i.IndicatorsPerformance).ToList());

        await _context.SaveChangesAsync();

        await UpdateOutputPerformance(subOutput.OutputCode);
    }

    private double CalculateAveragePerformance(List<double> performances)
    {
        return performances.Any() ? performances.Sum() / performances.Count : 0;
    }


    public async Task AddMeasureToProject(int projectId, int indicatorId, double value, MeasureValueType valueType)
    {
        var measure = new Measure
        {
            ProjectID = projectId,
            IndicatorCode = indicatorId,
            Value = value,
            ValueType = valueType,
            Date = DateTime.UtcNow
        };

        _context.Measures.Add(measure);
        await _context.SaveChangesAsync();

        if (valueType == MeasureValueType.Real)
        {
            await UpdateIndicatorPerformance(indicatorId);
        }
    }

    public async Task DeleteMeasureAndRecalculateAsync(int measureCode)
    {
        var measure = await _context.Measures
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Code == measureCode);

        if (measure == null)
            throw new InvalidOperationException($"Measure with code {measureCode} not found.");

        var indicatorId = measure.IndicatorCode;
        var projectId = measure.ProjectID;

        _context.Measures.Remove(measure);
        await _context.SaveChangesAsync();

        await UpdateIndicatorPerformance(indicatorId);
        await UpdateProjectPerformance(projectId);
        await UpdateMinistryPerformance(projectId);
    }

    /// <summary>
    /// Deletes a Project (cascade removes measures) and recalculates all related performance metrics.
    /// </summary>
    public async Task DeleteProjectAndRecalculateAsync(int projectId)
    {
        // 1. Load project with related Measures and Ministries
        var project = await _context.Projects
            .Include(p => p.Measures)
            .Include(p => p.Ministries)
            .FirstOrDefaultAsync(p => p.ProjectID == projectId);

        if (project == null)
            throw new InvalidOperationException($"Project with ID {projectId} not found.");

        // 2. Capture related IDs
        var indicatorIds = project.Measures.Select(m => m.IndicatorCode).Distinct().ToList();
        var ministryCodes = project.Ministries.Select(m => m.Code).Distinct().ToList();

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
    }

    /// <summary>
    /// Always recalc ministry performance by Ministry Code.
    /// </summary>
    private async Task UpdateMinistryPerformanceByMinistryCode(int ministryCode)
    {
        var ministry = await _context.Ministries
            .Include(m => m.Projects)
                .ThenInclude(p => p.Measures)
            .FirstOrDefaultAsync(m => m.Code == ministryCode);

        if (ministry == null)
            return;

        double totalTarget = 0.0;
        double totalReal = 0.0;

        foreach (var proj in ministry.Projects)
        {
            foreach (var measure in proj.Measures)
            {
                if (measure.ValueType == MeasureValueType.Real)
                    totalReal += measure.Value;
                else if (measure.ValueType == MeasureValueType.Target)
                    totalTarget += measure.Value;
            }
        }

        ministry.IndicatorsPerformance = totalTarget > 0
            ? (totalReal / totalTarget) * 100.0
            : 0.0;

        _context.Ministries.Update(ministry);
        await _context.SaveChangesAsync();
    }



}