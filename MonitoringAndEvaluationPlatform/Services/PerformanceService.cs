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

        // ========================================
        // DisbursementPerformance Methods
        // ========================================

        // 🔹 Update SubOutput DisbursementPerformance
        public async Task UpdateSubOutputDisbursementPerformance(int subOutputCode)
        {
            var subOutput = await _context.SubOutputs
                .Include(s => s.Indicators)
                .FirstOrDefaultAsync(s => s.Code == subOutputCode);

            if (subOutput == null) throw new Exception("SubOutput not found");

            // Only include indicators that have projects
            var indicatorsWithProjects = subOutput.Indicators
                .Where(i => _context.ProjectIndicators.Any(pi => pi.IndicatorCode == i.IndicatorCode))
                .ToList();

            if (indicatorsWithProjects.Any())
            {
                subOutput.DisbursementPerformance = (int)indicatorsWithProjects.Average(i => i.DisbursementPerformance);
                subOutput.FieldMonitoring = (int)indicatorsWithProjects.Average(i => i.FieldMonitoring);
                subOutput.ImpactAssessment = (int)indicatorsWithProjects.Average(i => i.ImpactAssessment);
            }
            else
            {
                subOutput.DisbursementPerformance = 0;
                subOutput.FieldMonitoring = 0;
                subOutput.ImpactAssessment = 0;
            }

            _context.SubOutputs.Update(subOutput);
            await _context.SaveChangesAsync();

            await UpdateOutputDisbursementPerformance(subOutput.OutputCode);
        }

        // 🔹 Update Output DisbursementPerformance
        public async Task UpdateOutputDisbursementPerformance(int outputCode)
        {
            var output = await _context.Outputs
                .Include(o => o.SubOutputs)
                    .ThenInclude(so => so.Indicators)
                .FirstOrDefaultAsync(o => o.Code == outputCode);

            if (output == null) throw new Exception("Output not found");

            // Only include subOutputs that have indicators with projects
            var subOutputsWithProjects = output.SubOutputs
                .Where(so => so.Indicators.Any(i => _context.ProjectIndicators.Any(pi => pi.IndicatorCode == i.IndicatorCode)))
                .ToList();

            if (subOutputsWithProjects.Any())
            {
                output.DisbursementPerformance = (int)subOutputsWithProjects.Average(so => so.DisbursementPerformance);
                output.FieldMonitoring = (int)subOutputsWithProjects.Average(so => so.FieldMonitoring);
                output.ImpactAssessment = (int)subOutputsWithProjects.Average(so => so.ImpactAssessment);
            }
            else
            {
                output.DisbursementPerformance = 0;
                output.FieldMonitoring = 0;
                output.ImpactAssessment = 0;
            }

            _context.Outputs.Update(output);
            await _context.SaveChangesAsync();

            await UpdateOutcomeDisbursementPerformance(output.OutcomeCode);
        }

        // 🔹 Update Outcome DisbursementPerformance
        public async Task UpdateOutcomeDisbursementPerformance(int outcomeCode)
        {
            var outcome = await _context.Outcomes
                .Include(o => o.Outputs)
                    .ThenInclude(o => o.SubOutputs)
                        .ThenInclude(so => so.Indicators)
                .FirstOrDefaultAsync(o => o.Code == outcomeCode);

            if (outcome == null) throw new Exception("Outcome not found");

            // Only include outputs that have subOutputs with indicators with projects
            var outputsWithProjects = outcome.Outputs
                .Where(o => o.SubOutputs.Any(so => so.Indicators.Any(i => _context.ProjectIndicators.Any(pi => pi.IndicatorCode == i.IndicatorCode))))
                .ToList();

            if (outputsWithProjects.Any())
            {
                outcome.DisbursementPerformance = (int)outputsWithProjects.Average(o => o.DisbursementPerformance);
                outcome.FieldMonitoring = (int)outputsWithProjects.Average(o => o.FieldMonitoring);
                outcome.ImpactAssessment = (int)outputsWithProjects.Average(o => o.ImpactAssessment);
            }
            else
            {
                outcome.DisbursementPerformance = 0;
                outcome.FieldMonitoring = 0;
                outcome.ImpactAssessment = 0;
            }

            _context.Outcomes.Update(outcome);
            await _context.SaveChangesAsync();

            await UpdateFrameworkDisbursementPerformance(outcome.FrameworkCode);
        }

        // 🔹 Update Framework DisbursementPerformance
        public async Task UpdateFrameworkDisbursementPerformance(int frameworkCode)
        {
            var framework = await _context.Frameworks
                .Include(f => f.Outcomes)
                    .ThenInclude(oc => oc.Outputs)
                        .ThenInclude(o => o.SubOutputs)
                            .ThenInclude(so => so.Indicators)
                .FirstOrDefaultAsync(f => f.Code == frameworkCode);

            if (framework == null) throw new Exception("Framework not found");

            // Only include outcomes that have outputs with subOutputs with indicators with projects
            var outcomesWithProjects = framework.Outcomes
                .Where(oc => oc.Outputs.Any(o => o.SubOutputs.Any(so => so.Indicators.Any(i => _context.ProjectIndicators.Any(pi => pi.IndicatorCode == i.IndicatorCode)))))
                .ToList();

            if (outcomesWithProjects.Any())
            {
                framework.DisbursementPerformance = (int)outcomesWithProjects.Average(oc => oc.DisbursementPerformance);
                framework.FieldMonitoring = (int)outcomesWithProjects.Average(oc => oc.FieldMonitoring);
                framework.ImpactAssessment = (int)outcomesWithProjects.Average(oc => oc.ImpactAssessment);
            }
            else
            {
                framework.DisbursementPerformance = 0;
                framework.FieldMonitoring = 0;
                framework.ImpactAssessment = 0;
            }

            _context.Frameworks.Update(framework);
            await _context.SaveChangesAsync();
        }
    }
}
