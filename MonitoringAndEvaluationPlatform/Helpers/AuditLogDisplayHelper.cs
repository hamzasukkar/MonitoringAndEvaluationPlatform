using System.Globalization;
using System.Text.Json;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Helpers
{
    public record AuditFieldChange(string Field, string? OldValue, string? NewValue);

    public static class AuditLogDisplayHelper
    {
        // Stored in the log but too noisy to show in change summaries
        private static readonly HashSet<string> NoiseFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "LastModifiedAt", "CreatedAt", "performance", "DisbursementPerformance"
        };

        private static readonly string[] NameKeyCandidates =
        {
            "Name", "Title", "UserName", "MinistryDisplayName"
        };

        public static bool IsNoiseField(string field) => NoiseFields.Contains(field);

        private static readonly HashSet<string> MoneyFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "EstimatedBudget", "RealBudget", "FundingAmount"
        };

        private static readonly Dictionary<string, string> CurrencySymbols = new(StringComparer.OrdinalIgnoreCase)
        {
            ["USD"] = "$",
            ["EUR"] = "€",
            ["SYP"] = "SYP "
        };

        // Relative-time descriptor: view renders Localizer[Key, Value];
        // "TimeOnly" means the view should print HH:mm itself (day shown by the group separator)
        public static (string Key, int Value) GetRelativeTime(DateTime utcTimestamp)
        {
            var local = utcTimestamp.ToLocalTime();
            var now = DateTime.Now;
            var age = now - local;

            if (age < TimeSpan.FromMinutes(1))
                return ("JustNow", 0);
            if (age < TimeSpan.FromHours(1))
                return ("MinutesAgo", (int)age.TotalMinutes);
            if (local.Date == now.Date)
                return ("HoursAgo", (int)age.TotalHours);

            return ("TimeOnly", 0);
        }

        // "Today" / "Yesterday" resource key, or null when the view should format the date itself
        public static string? GetDayKey(DateTime utcTimestamp)
        {
            var date = utcTimestamp.ToLocalTime().Date;
            var today = DateTime.Now.Date;

            if (date == today) return "Today";
            if (date == today.AddDays(-1)) return "Yesterday";
            return null;
        }

        public static List<AuditFieldChange> ParseChanges(string? oldJson, string? newJson, bool excludeNoise = true)
        {
            var oldValues = Deserialize(oldJson);
            var newValues = Deserialize(newJson);

            var fields = oldValues.Keys.Union(newValues.Keys, StringComparer.OrdinalIgnoreCase)
                .Where(f => !excludeNoise || !NoiseFields.Contains(f));

            var isUpdate = oldValues.Count > 0 && newValues.Count > 0;
            var currencySymbol = GetCurrencySymbol(newValues) ?? GetCurrencySymbol(oldValues);

            var changes = new List<AuditFieldChange>();
            foreach (var field in fields)
            {
                var oldValue = oldValues.TryGetValue(field, out var o) ? FormatValue(o) : null;
                var newValue = newValues.TryGetValue(field, out var n) ? FormatValue(n) : null;

                if (currencySymbol != null && MoneyFields.Contains(field))
                {
                    if (!string.IsNullOrEmpty(oldValue)) oldValue = currencySymbol + oldValue;
                    if (!string.IsNullOrEmpty(newValue)) newValue = currencySymbol + newValue;
                }

                // Older logs recorded properties EF flagged as modified even when
                // the value didn't actually change — hide those no-op entries
                if (isUpdate && string.Equals(oldValue, newValue, StringComparison.Ordinal))
                    continue;

                changes.Add(new AuditFieldChange(field, oldValue, newValue));
            }

            return changes;
        }

        public static string? FormatValue(JsonElement element, int maxLength = 0)
        {
            string? formatted = element.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Number => FormatNumber(element),
                JsonValueKind.String => FormatString(element.GetString()),
                _ => element.GetRawText()
            };

            if (formatted != null && maxLength > 0 && formatted.Length > maxLength)
                formatted = formatted.Substring(0, maxLength) + "…";

            return formatted;
        }

        public static string Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value ?? string.Empty;
            return value.Substring(0, maxLength) + "…";
        }

        // Fallback for logs written before EntityDisplayName existed:
        // pull a name-like key out of the stored JSON values.
        public static string? ExtractDisplayName(AuditLog log)
        {
            if (!string.IsNullOrWhiteSpace(log.EntityDisplayName))
                return log.EntityDisplayName;

            var values = Deserialize(log.NewValues);
            if (values.Count == 0)
                values = Deserialize(log.OldValues);

            var candidates = new List<string> { $"{log.EntityName}Name" };
            candidates.AddRange(NameKeyCandidates);

            foreach (var candidate in candidates)
            {
                var match = values.FirstOrDefault(kv =>
                    string.Equals(kv.Key, candidate, StringComparison.OrdinalIgnoreCase));
                if (match.Key != null && match.Value.ValueKind == JsonValueKind.String)
                {
                    var name = match.Value.GetString();
                    if (!string.IsNullOrWhiteSpace(name))
                        return name;
                }
            }

            return null;
        }

        // Controller route names for entity types that have a Details page
        private static readonly Dictionary<string, string> EntityControllers = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Project"] = "Projects",
            ["Ministry"] = "Ministries",
            ["Donor"] = "Donors",
            ["Sector"] = "Sectors",
            ["Framework"] = "Frameworks",
            ["Outcome"] = "Outcomes",
            ["Output"] = "Outputs",
            ["SubOutput"] = "SubOutputs",
            ["Indicator"] = "Indicators",
        };

        // Returns "/Controller/Details/id" for known entities with a real (positive) id
        public static string? TryGetEntityUrl(AuditLog log)
        {
            if (log.Action == "Delete")
                return null;

            if (!EntityControllers.TryGetValue(log.EntityName, out var controller))
                return null;

            // Create logs may hold a temporary negative id captured before the DB generated the key
            var id = new string(log.EntityId.Where(c => char.IsDigit(c) || c == '-').ToArray());
            if (!long.TryParse(id, out var numericId) || numericId <= 0)
                return null;

            return $"/{controller}/Details/{numericId}";
        }

        // Condenses a raw User-Agent string to "Browser · OS"; null when unrecognized
        public static string? FormatUserAgent(string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
                return null;

            string? browser = userAgent switch
            {
                var ua when ua.Contains("Edg/") => "Microsoft Edge",
                var ua when ua.Contains("OPR/") || ua.Contains("Opera") => "Opera",
                var ua when ua.Contains("Chrome/") => "Chrome",
                var ua when ua.Contains("Firefox/") => "Firefox",
                var ua when ua.Contains("Safari/") => "Safari",
                var ua when ua.Contains("curl/") => "curl",
                _ => null
            };

            string? os = userAgent switch
            {
                var ua when ua.Contains("Windows NT") => "Windows",
                var ua when ua.Contains("Android") => "Android",
                var ua when ua.Contains("iPhone") || ua.Contains("iPad") => "iOS",
                var ua when ua.Contains("Mac OS X") => "macOS",
                var ua when ua.Contains("Linux") => "Linux",
                _ => null
            };

            if (browser == null && os == null)
                return null;

            return browser != null && os != null ? $"{browser} · {os}" : browser ?? os;
        }

        private static string? GetCurrencySymbol(Dictionary<string, JsonElement> values)
        {
            var match = values.FirstOrDefault(kv =>
                string.Equals(kv.Key, "Currency", StringComparison.OrdinalIgnoreCase));

            if (match.Key == null || match.Value.ValueKind != JsonValueKind.String)
                return null;

            var currency = match.Value.GetString();
            return currency != null && CurrencySymbols.TryGetValue(currency, out var symbol) ? symbol : null;
        }

        private static Dictionary<string, JsonElement> Deserialize(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new Dictionary<string, JsonElement>();

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
                       ?? new Dictionary<string, JsonElement>();
            }
            catch
            {
                return new Dictionary<string, JsonElement>();
            }
        }

        private static string FormatNumber(JsonElement element)
        {
            if (element.TryGetInt64(out var intValue))
                return intValue.ToString("N0", CultureInfo.InvariantCulture);

            if (element.TryGetDouble(out var doubleValue))
                return doubleValue.ToString("N2", CultureInfo.InvariantCulture);

            return element.GetRawText();
        }

        private static string? FormatString(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // ISO date/datetime strings → short date
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date)
                && (value.Contains('T') || value.Length == 10))
            {
                return date.TimeOfDay == TimeSpan.Zero
                    ? date.ToString("yyyy-MM-dd")
                    : date.ToString("yyyy-MM-dd HH:mm");
            }

            return value;
        }
    }
}
