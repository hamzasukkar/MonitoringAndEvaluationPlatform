using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Services
{
    /// <summary>
    /// A budget total expressed in Syrian Pounds, plus whatever could not be converted.
    /// </summary>
    /// <remarks>
    /// Amounts that cannot be converted are never silently dropped — a total that quietly
    /// omits projects reads as complete and under-reports. Callers surface
    /// <see cref="UnconvertedByCurrency"/> so the gap is visible.
    /// </remarks>
    public readonly record struct MoneyTotal(
        double Syp,
        int UnconvertedCount,
        IReadOnlyDictionary<string, double> UnconvertedByCurrency)
    {
        public static readonly MoneyTotal Empty =
            new(0, 0, new Dictionary<string, double>());

        /// <summary>True when every amount in the set was converted.</summary>
        public bool IsComplete => UnconvertedCount == 0;
    }

    /// <summary>
    /// An immutable snapshot of the platform's exchange rates. Holds no DbContext, so it is
    /// safe to pass around and use inside tight loops after a single async fetch.
    /// </summary>
    public sealed class CurrencyConverter
    {
        public const string BaseCurrency = "SYP";

        private readonly IReadOnlyDictionary<string, decimal> _fallbackRates;

        public CurrencyConverter(IReadOnlyDictionary<string, decimal> fallbackRates)
        {
            _fallbackRates = fallbackRates;
        }

        /// <summary>
        /// Syrian Pounds per one unit of <paramref name="currency"/>, or null when the amount
        /// cannot be converted (unknown/blank currency with no platform rate).
        /// </summary>
        /// <param name="projectRate">
        /// The project's own captured rate. Wins over the platform rate, because it is the
        /// snapshot taken when that project was entered.
        /// </param>
        public double? FactorFor(string? currency, decimal? projectRate)
        {
            if (string.IsNullOrWhiteSpace(currency))
                return null;

            if (string.Equals(currency, BaseCurrency, StringComparison.OrdinalIgnoreCase))
                return 1d;

            if (projectRate is > 0)
                return (double)projectRate.Value;

            if (_fallbackRates.TryGetValue(currency, out var rate) && rate > 0)
                return (double)rate;

            return null;
        }

        public double? ToSyp(double amount, string? currency, decimal? projectRate)
        {
            var factor = FactorFor(currency, projectRate);
            return factor is null ? null : amount * factor.Value;
        }

        /// <summary>Sums an arbitrary money field across projects, converting each to SYP.</summary>
        public MoneyTotal Sum(IEnumerable<Project> projects, Func<Project, double> selector)
        {
            double total = 0;
            var unconverted = new Dictionary<string, double>();
            var unconvertedCount = 0;

            foreach (var project in projects)
            {
                var amount = selector(project);
                var factor = FactorFor(project.Currency, project.ExchangeRate);

                if (factor is null)
                {
                    unconvertedCount++;
                    var key = string.IsNullOrWhiteSpace(project.Currency) ? "?" : project.Currency;
                    unconverted[key] = unconverted.GetValueOrDefault(key) + amount;
                    continue;
                }

                total += amount * factor.Value;
            }

            return new MoneyTotal(total, unconvertedCount, unconverted);
        }

        public MoneyTotal SumBudget(IEnumerable<Project> projects) =>
            Sum(projects, p => p.EstimatedBudget);

        public MoneyTotal SumRealBudget(IEnumerable<Project> projects) =>
            Sum(projects, p => p.RealBudget);
    }

    public interface ICurrencyConversionService
    {
        /// <summary>Loads the current rates once; reuse the result for a whole request.</summary>
        Task<CurrencyConverter> GetConverterAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Sums <see cref="Project.EstimatedBudget"/> over a query without materialising the
        /// projects — aggregation happens in SQL, conversion in memory.
        /// </summary>
        Task<MoneyTotal> SumBudgetToSypAsync(
            IQueryable<Project> query, CancellationToken cancellationToken = default);

        /// <summary>Platform fallback rates keyed by currency code, for view-side prefill.</summary>
        Task<IReadOnlyDictionary<string, decimal>> GetFallbackRatesAsync(
            CancellationToken cancellationToken = default);

        /// <summary>Drops the cached rates after an admin edits them.</summary>
        void InvalidateCache();
    }
}
