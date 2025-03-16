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
        var framework = await _context.Framework.FirstOrDefaultAsync(i => i.Code == frameworkCode);

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
}