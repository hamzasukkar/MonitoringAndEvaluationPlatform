using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;

public class PlanService
{
    private readonly ApplicationDbContext _context;

    public PlanService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task UpdatePlanAsync(Plan updatedPlan)
    {
        // Get the existing Plan with relationships loaded
        var existingPlan = await _context.Plans
            .Include(p => p.Activity)
                .ThenInclude(a => a.ActionPlan)
                    .ThenInclude(ap => ap.Project)
            .FirstOrDefaultAsync(p => p.Code == updatedPlan.Code);

        if (existingPlan == null)
        {
            throw new Exception("Plan not found.");
        }

        // Update fields
        existingPlan.Name = updatedPlan.Name;
        existingPlan.Date = updatedPlan.Date;
        existingPlan.Planned = updatedPlan.Planned;
        existingPlan.Realised = updatedPlan.Realised;

        _context.Update(existingPlan);
        await _context.SaveChangesAsync();

        // Call method to update performance of ActionPlan and Project
        existingPlan.Activity?.ActionPlan?.UpdatePerformance();

        // Save updated performance values
        await _context.SaveChangesAsync();
    }

    public async Task<bool> PlanExistsAsync(int id)
    {
        return await _context.Plans.AnyAsync(p => p.Code == id);
    }
}
