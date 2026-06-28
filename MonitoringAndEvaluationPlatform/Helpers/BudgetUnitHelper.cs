using System.Globalization;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Helpers
{
    public static class BudgetUnitHelper
    {
        /// <summary>
        /// The multiplier between the scaled (entry/display) value and the full stored value.
        /// Falls back to 1 for an undefined/zero unit so it can never zero out a budget.
        /// </summary>
        public static long Multiplier(this BudgetUnit unit)
        {
            var m = (long)unit;
            return m > 0 ? m : 1;
        }

        /// <summary>Short suffix shown next to a scaled value ("", "K", "M").</summary>
        public static string Suffix(this BudgetUnit unit) => unit switch
        {
            BudgetUnit.Thousands => "K",
            BudgetUnit.Millions => "M",
            BudgetUnit.Billions => "B",
            _ => string.Empty
        };

        /// <summary>
        /// Formats a full monetary value scaled down to the given unit, e.g. 5,000,000 + Millions => "5 M".
        /// Trims trailing zeros so whole scaled values show without a decimal part.
        /// </summary>
        public static string ToScaledString(double fullValue, BudgetUnit unit)
        {
            var scaled = fullValue / unit.Multiplier();
            // Up to 2 decimals, but drop them when the scaled value is whole.
            var text = scaled.ToString("#,##0.##", CultureInfo.InvariantCulture);
            var suffix = unit.Suffix();
            return string.IsNullOrEmpty(suffix) ? text : $"{text} {suffix}";
        }
    }
}
