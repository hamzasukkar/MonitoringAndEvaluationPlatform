using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringAndEvaluationPlatform.Models
{
    /// <summary>
    /// A platform-wide fallback exchange rate, one row per supported currency.
    /// </summary>
    /// <remarks>
    /// This is the *fallback* used when a project carries no rate of its own — mainly legacy
    /// rows created before the rate became mandatory for non-SYP currencies. A project's own
    /// <see cref="Project.RateToSyp"/> always wins, because it is the historical snapshot taken
    /// when that project was entered. Deliberately holds no history: the per-project rate already
    /// serves that purpose, and a temporal lookup would leak into every aggregation site.
    /// </remarks>
    public class CurrencyRate
    {
        /// <summary>ISO 4217 code, e.g. "USD". Also the primary key.</summary>
        [Key]
        [StringLength(3)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// How many Syrian Pounds one unit of this currency is worth. SYP itself is 1.
        /// </summary>
        [Range(0.0001, double.MaxValue)]
        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Rate to SYP")]
        public decimal RateToSyp { get; set; }

        public string NameEn { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;

        /// <summary>Symbol shown next to amounts in this currency, e.g. "$".</summary>
        public string Symbol { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; }

        public string? UpdatedBy { get; set; }
    }
}
