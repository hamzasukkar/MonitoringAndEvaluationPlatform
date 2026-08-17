using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Services
{
    public class CurrencyConversionService : ICurrencyConversionService
    {
        private const string CacheKey = "CurrencyRates:All";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public CurrencyConversionService(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<IReadOnlyDictionary<string, decimal>> GetFallbackRatesAsync(
            CancellationToken cancellationToken = default)
        {
            if (_cache.TryGetValue(CacheKey, out IReadOnlyDictionary<string, decimal>? cached) && cached is not null)
                return cached;

            var rates = await _context.CurrencyRates
                .AsNoTracking()
                .ToDictionaryAsync(r => r.Code, r => r.RateToSyp, StringComparer.OrdinalIgnoreCase, cancellationToken);

            _cache.Set(CacheKey, (IReadOnlyDictionary<string, decimal>)rates, CacheDuration);
            return rates;
        }

        public async Task<CurrencyConverter> GetConverterAsync(CancellationToken cancellationToken = default)
        {
            var rates = await GetFallbackRatesAsync(cancellationToken);
            return new CurrencyConverter(rates);
        }

        public async Task<MoneyTotal> SumBudgetToSypAsync(
            IQueryable<Project> query, CancellationToken cancellationToken = default)
        {
            // A conversion factor cannot be resolved inside an expression tree, and a SQL CASE
            // would lose the "which projects could not be converted" signal. So aggregate in
            // SQL, grouped by the two inputs the factor depends on, then convert the handful of
            // resulting buckets in memory. One round trip, bounded row count.
            var buckets = await query
                .GroupBy(p => new { p.Currency, p.ExchangeRate })
                .Select(g => new
                {
                    g.Key.Currency,
                    g.Key.ExchangeRate,
                    Amount = g.Sum(x => x.EstimatedBudget),
                    Count = g.Count()
                })
                .ToListAsync(cancellationToken);

            var converter = await GetConverterAsync(cancellationToken);

            double total = 0;
            var unconverted = new Dictionary<string, double>();
            var unconvertedCount = 0;

            foreach (var bucket in buckets)
            {
                var factor = converter.FactorFor(bucket.Currency, bucket.ExchangeRate);

                if (factor is null)
                {
                    unconvertedCount += bucket.Count;
                    var key = string.IsNullOrWhiteSpace(bucket.Currency) ? "?" : bucket.Currency;
                    unconverted[key] = unconverted.GetValueOrDefault(key) + bucket.Amount;
                    continue;
                }

                total += bucket.Amount * factor.Value;
            }

            return new MoneyTotal(total, unconvertedCount, unconverted);
        }

        public void InvalidateCache() => _cache.Remove(CacheKey);
    }
}
