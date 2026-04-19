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

    // ─────────────────────────────────────────────────────────────────────────
    // PHASE PERFORMANCE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Recalculates a phase's performance from its measures (simple average of values),
    /// then cascades up to the project and the full hierarchy.
    /// </summary>
    public async Task UpdatePhasePerformance(int phaseId)
    {
        var phase = await _context.ProjectPhases
            .Include(pp => pp.Measures)
            .FirstOrDefaultAsync(pp => pp.Id == phaseId);

        if (phase == null) return;

        phase.PhasePerformance = phase.Measures.Any()
            ? phase.Measures.Sum(m => m.Value)
            : 0;

        _context.ProjectPhases.Update(phase);
        await _context.SaveChangesAsync();

        await UpdateProjectPerformanceFromPhases(phase.ProjectID);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PROJECT PERFORMANCE (phase-weighted)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Calculates project performance as a weighted average of all phase performances.
    /// Weight for each phase is its Weight property (sum of all weights = 100).
    /// </summary>
    public async Task UpdateProjectPerformanceFromPhases(int projectId)
    {
        var project = await _context.Projects
            .Include(p => p.Phases)
            .FirstOrDefaultAsync(p => p.ProjectID == projectId);

        if (project == null) return;

        double totalWeight = (double)project.Phases.Sum(pp => pp.Weight);
        project.performance = totalWeight > 0
            ? project.Phases.Sum(pp => pp.PhasePerformance * (double)pp.Weight) / totalWeight
            : 0;

        _context.Projects.Update(project);
        await _context.SaveChangesAsync();

        // Cascade to indicators (passthrough) → hierarchy
        await UpdateIndicatorsForProject(projectId);
        await UpdateCrossCuttingEntitiesForProject(projectId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // INDICATOR PERFORMANCE (passthrough from project)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Updates all indicators linked to a project. Their performance = project.performance (passthrough).
    /// Then cascades up the SubOutput → Output → Outcome → Framework hierarchy.
    /// </summary>
    private async Task UpdateIndicatorsForProject(int projectId)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.ProjectID == projectId);

        if (project == null) return;

        var indicators = await _context.Indicators
            .Where(i => i.ProjectID == projectId)
            .ToListAsync();

        var affectedSubOutputCodes = new HashSet<int>();

        foreach (var indicator in indicators)
        {
            indicator.IndicatorsPerformance = project.performance;
            _context.Indicators.Update(indicator);
            if (indicator.SubOutputCode != 0)
                affectedSubOutputCodes.Add(indicator.SubOutputCode);
        }

        await _context.SaveChangesAsync();

        foreach (var subOutputCode in affectedSubOutputCodes)
        {
            await UpdateSubOutputPerformance(subOutputCode);
        }
    }

    /// <summary>
    /// Public version: updates a single indicator's performance (passthrough from its linked project).
    /// Called after project performance changes or when an indicator's project link changes.
    /// </summary>
    public async Task UpdateIndicatorPerformance(int indicatorId)
    {
        var indicator = await _context.Indicators
            .Include(i => i.Project)
            .FirstOrDefaultAsync(i => i.IndicatorCode == indicatorId);

        if (indicator == null) return;

        indicator.IndicatorsPerformance = indicator.Project?.performance ?? 0;
        _context.Indicators.Update(indicator);
        await _context.SaveChangesAsync();

        if (indicator.SubOutputCode != 0)
            await UpdateSubOutputPerformance(indicator.SubOutputCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HIERARCHY CASCADE (SubOutput → Output → Outcome → Framework)
    // ─────────────────────────────────────────────────────────────────────────

    private async Task UpdateSubOutputPerformance(int subOutputCode)
    {
        var subOutput = await _context.SubOutputs.FirstOrDefaultAsync(s => s.Code == subOutputCode);
        if (subOutput == null) return;

        var indicators = await _context.Indicators
            .Where(i => i.SubOutputCode == subOutputCode)
            .ToListAsync();

        subOutput.IndicatorsPerformance = CalculateWeightedAverage(
            indicators, i => i.IndicatorsPerformance, i => i.Weight);

        await _context.SaveChangesAsync();
        await UpdateOutputPerformance(subOutput.OutputCode);
    }

    private async Task UpdateOutputPerformance(int outputCode)
    {
        var output = await _context.Outputs.FirstOrDefaultAsync(o => o.Code == outputCode);
        if (output == null) return;

        var subOutputs = await _context.SubOutputs
            .Where(s => s.OutputCode == outputCode)
            .ToListAsync();

        output.IndicatorsPerformance = CalculateWeightedAverage(
            subOutputs, s => s.IndicatorsPerformance, s => s.Weight);

        await _context.SaveChangesAsync();
        await UpdateOutcomePerformance(output.OutcomeCode);
    }

    private async Task UpdateOutcomePerformance(int outcomeCode)
    {
        var outcome = await _context.Outcomes.FirstOrDefaultAsync(o => o.Code == outcomeCode);
        if (outcome == null) return;

        var outputs = await _context.Outputs
            .Where(o => o.OutcomeCode == outcomeCode)
            .ToListAsync();

        outcome.IndicatorsPerformance = CalculateWeightedAverage(
            outputs, o => o.IndicatorsPerformance, o => o.Weight);

        await _context.SaveChangesAsync();
        await UpdateFrameworkPerformance(outcome.FrameworkCode);
    }

    private async Task UpdateFrameworkPerformance(int frameworkCode)
    {
        var framework = await _context.Frameworks.FirstOrDefaultAsync(f => f.Code == frameworkCode);
        if (framework == null) return;

        var outcomes = await _context.Outcomes
            .Where(o => o.FrameworkCode == frameworkCode)
            .ToListAsync();

        framework.IndicatorsPerformance = CalculateWeightedAverage(
            outcomes, o => o.IndicatorsPerformance, o => o.Weight);

        await _context.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CROSS-CUTTING ENTITIES (Ministry, Sector, Donor) — IndicatorsPerformance
    // ─────────────────────────────────────────────────────────────────────────

    private async Task UpdateCrossCuttingEntitiesForProject(int projectId)
    {
        var project = await _context.Projects
            .Include(p => p.Ministries)
            .Include(p => p.Sectors)
            .Include(p => p.Donors)
            .FirstOrDefaultAsync(p => p.ProjectID == projectId);

        if (project == null) return;

        foreach (var ministry in project.Ministries)
            await UpdateMinistryPerformanceByMinistryCode(ministry.Code);

        foreach (var sector in project.Sectors)
            await UpdateSectorPerformanceBySectorId(sector.Code);

        foreach (var donor in project.Donors)
            await UpdateDonorPerformanceByDonorCode(donor.Code);
    }

    private async Task UpdateMinistryPerformanceByMinistryCode(int ministryCode)
    {
        var ministry = await _context.Ministries
            .Include(m => m.Projects)
            .FirstOrDefaultAsync(m => m.Code == ministryCode);

        if (ministry == null) return;

        ministry.IndicatorsPerformance = ministry.Projects.Any()
            ? ministry.Projects.Average(p => p.performance)
            : 0;

        _context.Ministries.Update(ministry);
        await _context.SaveChangesAsync();
    }

    private async Task UpdateSectorPerformanceBySectorId(int sectorId)
    {
        var sector = await _context.Sectors
            .Include(s => s.Projects)
            .FirstOrDefaultAsync(s => s.Code == sectorId);

        if (sector == null) return;

        sector.IndicatorsPerformance = sector.Projects.Any()
            ? sector.Projects.Average(p => p.performance)
            : 0;

        _context.Sectors.Update(sector);
        await _context.SaveChangesAsync();
    }

    private async Task UpdateDonorPerformanceByDonorCode(int donorCode)
    {
        var donor = await _context.Donors
            .Include(d => d.Projects)
            .FirstOrDefaultAsync(d => d.Code == donorCode);

        if (donor == null) return;

        donor.IndicatorsPerformance = donor.Projects.Any()
            ? donor.Projects.Average(p => p.performance)
            : 0;

        _context.Donors.Update(donor);
        await _context.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DISBURSEMENT PERFORMANCE (ActionPlan/Plans based, now via phases)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Updates disbursement-type performances for a project by aggregating plans
    /// across all phases' action plans. Also cascades to Indicators, Ministries, Sectors, Donors.
    /// </summary>
    public async Task UpdateDisbursementPerformancesForProject(int projectId)
    {
        var project = await _context.Projects
            .Include(p => p.Phases)
                .ThenInclude(pp => pp.ActionPlan)
                    .ThenInclude(ap => ap.Activities)
                        .ThenInclude(a => a.Plans)
            .Include(p => p.Ministries)
            .Include(p => p.Sectors)
            .Include(p => p.Donors)
            .FirstOrDefaultAsync(p => p.ProjectID == projectId);

        if (project == null) return;

        // Aggregate plans across all phases
        var allPlans = project.Phases
            .Where(pp => pp.ActionPlan != null)
            .SelectMany(pp => pp.ActionPlan!.Activities)
            .SelectMany(a => a.Plans)
            .ToList();

        project.DisbursementPerformance = CalcDisbursementPerf(allPlans, project.EstimatedBudget);

        _context.Projects.Update(project);
        await _context.SaveChangesAsync();

        // Cascade to linked indicators (disbursement passthrough)
        var indicators = await _context.Indicators
            .Where(i => i.ProjectID == projectId)
            .ToListAsync();

        foreach (var indicator in indicators)
        {
            indicator.DisbursementPerformance = project.DisbursementPerformance;
            _context.Indicators.Update(indicator);
        }
        await _context.SaveChangesAsync();

        // Cascade DisbursementPerformance up the hierarchy via the first affected subOutput
        var subOutputCodes = indicators.Select(i => i.SubOutputCode).Distinct().Where(c => c != 0).ToList();
        foreach (var subOutputCode in subOutputCodes)
        {
            await UpdateSubOutputDisbursementPerformance(subOutputCode);
        }

        // Update cross-cutting disbursement
        foreach (var ministry in project.Ministries)
            await UpdateMinistryDisbursementPerformanceByCode(ministry.Code);
        foreach (var sector in project.Sectors)
            await UpdateSectorDisbursementPerformanceByCode(sector.Code);
        foreach (var donor in project.Donors)
            await UpdateDonorDisbursementPerformanceByCode(donor.Code);
    }

    private double CalcDisbursementPerf(List<Plan> plans, double estimatedBudget)
    {
        double totalRealised = plans.Sum(p => p.Realised);
        return estimatedBudget > 0 ? (totalRealised / estimatedBudget) * 100 : 0;
    }

    private async Task UpdateSubOutputDisbursementPerformance(int subOutputCode)
    {
        var subOutput = await _context.SubOutputs.FirstOrDefaultAsync(s => s.Code == subOutputCode);
        if (subOutput == null) return;

        var indicators = await _context.Indicators
            .Where(i => i.SubOutputCode == subOutputCode && i.ProjectID != null)
            .ToListAsync();

        subOutput.DisbursementPerformance = indicators.Any() ? indicators.Average(i => i.DisbursementPerformance) : 0;

        await _context.SaveChangesAsync();

        if (subOutput.OutputCode != 0)
            await UpdateOutputDisbursementPerformance(subOutput.OutputCode);
    }

    private async Task UpdateOutputDisbursementPerformance(int outputCode)
    {
        var output = await _context.Outputs.FirstOrDefaultAsync(o => o.Code == outputCode);
        if (output == null) return;

        var subOutputs = await _context.SubOutputs
            .Where(s => s.OutputCode == outputCode)
            .ToListAsync();

        output.DisbursementPerformance = subOutputs.Any() ? subOutputs.Average(s => s.DisbursementPerformance) : 0;

        await _context.SaveChangesAsync();

        if (output.OutcomeCode != 0)
            await UpdateOutcomeDisbursementPerformance(output.OutcomeCode);
    }

    private async Task UpdateOutcomeDisbursementPerformance(int outcomeCode)
    {
        var outcome = await _context.Outcomes.FirstOrDefaultAsync(o => o.Code == outcomeCode);
        if (outcome == null) return;

        var outputs = await _context.Outputs
            .Where(o => o.OutcomeCode == outcomeCode)
            .ToListAsync();

        outcome.DisbursementPerformance = outputs.Any() ? outputs.Average(o => o.DisbursementPerformance) : 0;

        await _context.SaveChangesAsync();

        if (outcome.FrameworkCode != 0)
            await UpdateFrameworkDisbursementPerformance(outcome.FrameworkCode);
    }

    private async Task UpdateFrameworkDisbursementPerformance(int frameworkCode)
    {
        var framework = await _context.Frameworks.FirstOrDefaultAsync(f => f.Code == frameworkCode);
        if (framework == null) return;

        var outcomes = await _context.Outcomes
            .Where(o => o.FrameworkCode == frameworkCode)
            .ToListAsync();

        framework.DisbursementPerformance = outcomes.Any() ? outcomes.Average(o => o.DisbursementPerformance) : 0;

        await _context.SaveChangesAsync();
    }

    private async Task UpdateMinistryDisbursementPerformanceByCode(int ministryCode)
    {
        var ministry = await _context.Ministries
            .Include(m => m.Projects)
            .FirstOrDefaultAsync(m => m.Code == ministryCode);
        if (ministry == null) return;

        ministry.DisbursementPerformance = ministry.Projects.Any() ? ministry.Projects.Average(p => p.DisbursementPerformance) : 0;

        _context.Ministries.Update(ministry);
        await _context.SaveChangesAsync();
    }

    private async Task UpdateSectorDisbursementPerformanceByCode(int sectorCode)
    {
        var sector = await _context.Sectors
            .Include(s => s.Projects)
            .FirstOrDefaultAsync(s => s.Code == sectorCode);
        if (sector == null) return;

        sector.DisbursementPerformance = sector.Projects.Any() ? sector.Projects.Average(p => p.DisbursementPerformance) : 0;

        _context.Sectors.Update(sector);
        await _context.SaveChangesAsync();
    }

    private async Task UpdateDonorDisbursementPerformanceByCode(int donorCode)
    {
        var donor = await _context.Donors
            .Include(d => d.Projects)
            .FirstOrDefaultAsync(d => d.Code == donorCode);
        if (donor == null) return;

        donor.DisbursementPerformance = donor.Projects.Any() ? donor.Projects.Average(p => p.DisbursementPerformance) : 0;

        _context.Donors.Update(donor);
        await _context.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MEASURE CRUD HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    public async Task AddMeasureToPhase(int phaseId, double value, string name = "", string? note = null, double? quantity = null, string? unit = null, MeasureType measureType = MeasureType.Qualitative)
    {
        var measure = new Measure
        {
            ProjectPhaseId = phaseId,
            Value = value,
            Date = DateTime.UtcNow,
            Name = name,
            Note = note,
            Quantity = quantity,
            Unit = unit,
            MeasureType = measureType
        };

        _context.Measures.Add(measure);
        await _context.SaveChangesAsync();

        await UpdatePhasePerformance(phaseId);
    }

    public async Task DeleteMeasureAndRecalculateAsync(int measureCode)
    {
        var measure = await _context.Measures
            .Include(m => m.ProjectPhase)
                .ThenInclude(pp => pp.Project)
                    .ThenInclude(p => p.Ministries)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Code == measureCode);

        if (measure == null)
            throw new InvalidOperationException($"Measure with code {measureCode} not found.");

        var phaseId = measure.ProjectPhaseId;

        _context.Measures.Remove(measure);
        await _context.SaveChangesAsync();

        await UpdatePhasePerformance(phaseId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PROJECT DELETE RECALCULATION
    // ─────────────────────────────────────────────────────────────────────────

    public async Task DeleteProjectAndRecalculateAsync(int projectId)
    {
        var project = await _context.Projects
            .Include(p => p.Indicators)
            .Include(p => p.Ministries)
            .Include(p => p.Sectors)
            .Include(p => p.Donors)
            .FirstOrDefaultAsync(p => p.ProjectID == projectId);

        if (project == null)
            throw new InvalidOperationException($"Project with ID {projectId} not found.");

        var indicatorIds = project.Indicators.Select(i => i.IndicatorCode).ToList();
        var subOutputCodes = project.Indicators.Select(i => i.SubOutputCode).Distinct().Where(c => c != 0).ToList();
        var ministryCodes = project.Ministries.Select(m => m.Code).ToList();
        var sectorIds = project.Sectors.Select(s => s.Code).ToList();
        var donorCodes = project.Donors.Select(d => d.Code).ToList();

        // Delete linked indicators together with the project (they are created and exist as a pair)
        _context.Indicators.RemoveRange(project.Indicators);
        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();

        // Cascade up the hierarchy from affected subOutputs
        foreach (var subOutputCode in subOutputCodes)
            await UpdateSubOutputPerformance(subOutputCode);

        foreach (var ministryCode in ministryCodes)
            await UpdateMinistryPerformanceByMinistryCode(ministryCode);

        foreach (var sectorId in sectorIds)
            await UpdateSectorPerformanceBySectorId(sectorId);

        foreach (var donorCode in donorCodes)
            await UpdateDonorPerformanceByDonorCode(donorCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC WRAPPERS for cross-cutting (used by other controllers)
    // ─────────────────────────────────────────────────────────────────────────

    public async Task UpdateMinistryPerformance(int projectId)
    {
        var project = await _context.Projects
            .Include(p => p.Ministries)
            .FirstOrDefaultAsync(p => p.ProjectID == projectId);
        if (project == null) return;
        foreach (var ministry in project.Ministries)
            await UpdateMinistryPerformanceByMinistryCode(ministry.Code);
    }

    public async Task UpdateSectorPerformance(int projectId)
    {
        var project = await _context.Projects
            .Include(p => p.Sectors)
            .FirstOrDefaultAsync(p => p.ProjectID == projectId);
        if (project == null) return;
        foreach (var sector in project.Sectors)
            await UpdateSectorPerformanceBySectorId(sector.Code);
    }

    public async Task UpdateDonorPerformance(int projectId)
    {
        var project = await _context.Projects
            .Include(p => p.Donors)
            .FirstOrDefaultAsync(p => p.ProjectID == projectId);
        if (project == null) return;
        foreach (var donor in project.Donors)
            await UpdateDonorPerformanceByDonorCode(donor.Code);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    private double CalculateWeightedAverage<T>(List<T> items, Func<T, double> valueSelector, Func<T, double> weightSelector)
    {
        var totalWeight = items.Sum(weightSelector);
        return totalWeight > 0 ? items.Sum(i => valueSelector(i) * weightSelector(i)) / totalWeight : 0;
    }
}
