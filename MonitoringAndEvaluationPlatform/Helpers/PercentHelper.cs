using System.Globalization;

namespace MonitoringAndEvaluationPlatform.Helpers
{
    /// <summary>
    /// Formatting for the 0-100 weight and performance values. These are stored as <see cref="double"/>,
    /// so an equal split leaves binary-floating-point residue (12 indicators give 8.370000000000006).
    /// Rendering through here keeps already-stored values readable without a data migration.
    /// </summary>
    public static class PercentHelper
    {
        // Up to 2 decimals, but drop them when the value is whole: 8.37 stays "8.37", 25.0 becomes "25".
        private const string Format = "0.##";

        /// <summary>Formats a percentage value for display, without a "%" sign.</summary>
        public static string ToPercentString(this double value) =>
            Math.Round(value, 2).ToString(Format, CultureInfo.InvariantCulture);

        /// <summary>Decimal overload, so callers do not have to care which type a weight column uses.</summary>
        public static string ToPercentString(this decimal value) =>
            Math.Round(value, 2).ToString(Format, CultureInfo.InvariantCulture);

        /// <summary>Rounds a weight to the 2 decimals the UI shows, so stored values match what users see.</summary>
        public static double RoundWeight(double value) => Math.Round(value, 2);
    }
}
