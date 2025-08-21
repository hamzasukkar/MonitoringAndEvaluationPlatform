using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;

namespace MonitoringAndEvaluationPlatform.Services
{
    public class PerformanceService : IPerformanceService
    {
        private readonly ApplicationDbContext _context;

        public PerformanceService(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🔹 Helper Method عام
        private double CalculateWeightedPerformance<T>(
            IEnumerable<T> items,
            Func<T, double> performanceSelector,
            Func<T, double> weightSelector)
        {
            if (items == null || !items.Any()) return 0;

            double totalWeight = items.Sum(weightSelector);
            if (totalWeight <= 0) totalWeight = items.Count(); // fallback

            double weightedPerformance = items.Sum(i => performanceSelector(i) * weightSelector(i) / totalWeight);
            return Math.Round(weightedPerformance, 2);
        }

        // 🔹 Update SubOutput
        public async Task UpdateSubOutputPerformance(int subOutputCode)
        {
            var subOutput = await _context.SubOutputs
                .Include(s => s.Indicators)
                .FirstOrDefaultAsync(s => s.Code == subOutputCode);

            if (subOutput == null) throw new Exception("SubOutput not found");

            subOutput.IndicatorsPerformance = CalculateWeightedPerformance(
                subOutput.Indicators,
                i => i.IndicatorsPerformance,
                i => i.Weight
            );

            _context.SubOutputs.Update(subOutput);
            await _context.SaveChangesAsync();

            await UpdateOutputPerformance(subOutput.OutputCode);
        }

        // 🔹 Update Output
        public async Task UpdateOutputPerformance(int outputCode)
        {
            var output = await _context.Outputs
                .Include(o => o.SubOutputs)
                .FirstOrDefaultAsync(o => o.Code == outputCode);

            if (output == null) throw new Exception("Output not found");

            output.IndicatorsPerformance = CalculateWeightedPerformance(
                output.SubOutputs,
                s => s.IndicatorsPerformance,
                s => s.Weight
            );

            _context.Outputs.Update(output);
            await _context.SaveChangesAsync();

            await UpdateOutcomePerformance(output.OutcomeCode);
        }

        // 🔹 Update Outcome
        public async Task UpdateOutcomePerformance(int outcomeCode)
        {
            var outcome = await _context.Outcomes
                .Include(o => o.Outputs)
                .FirstOrDefaultAsync(o => o.Code == outcomeCode);

            if (outcome == null) throw new Exception("Outcome not found");

            outcome.IndicatorsPerformance = CalculateWeightedPerformance(
                outcome.Outputs,
                o => o.IndicatorsPerformance,
                o => o.Weight
            );

            _context.Outcomes.Update(outcome);
            await _context.SaveChangesAsync();

            await UpdateFrameworkPerformance(outcome.FrameworkCode);
        }

        // 🔹 Update Framework
        public async Task UpdateFrameworkPerformance(int frameworkCode)
        {
            var framework = await _context.Frameworks
                .Include(f => f.Outcomes)
                .FirstOrDefaultAsync(f => f.Code == frameworkCode);

            if (framework == null) throw new Exception("Framework not found");

            framework.IndicatorsPerformance = CalculateWeightedPerformance(
                framework.Outcomes,
                o => o.IndicatorsPerformance,
                o => o.Weight
            );

            _context.Frameworks.Update(framework);
            await _context.SaveChangesAsync();
        }
    }
}
