namespace MonitoringAndEvaluationPlatform.Models
{
    /// <summary>
    /// Display/entry scale for a project's budget. The stored EstimatedBudget is always
    /// the full monetary value; this unit only controls how that value is entered and shown.
    /// The numeric value of each member is its multiplier.
    /// </summary>
    public enum BudgetUnit
    {
        Ones = 1,
        Thousands = 1000,
        Millions = 1000000,
        Billions = 1000000000
    }
}
