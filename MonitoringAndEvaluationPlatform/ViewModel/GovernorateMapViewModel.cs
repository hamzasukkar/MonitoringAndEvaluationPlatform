namespace MonitoringAndEvaluationPlatform.ViewModel
{
    /// <summary>
    /// ViewModel for the projects map report (Reports/GovernorateMap), which serves BOTH admin
    /// levels: governorate (a stylised SVG of Syria) and district (Leaflet over ADM2 boundaries).
    ///
    /// The two used to be separate pages with separate view models that differed only by the
    /// district list — everything else, including the whole filter bar and projects table, was
    /// duplicated. They are one page with a level toggle now; <see cref="Level"/> says which
    /// renderer starts visible.
    ///
    /// A flat projects list drives the table, map colouring, highlighting and the cascading
    /// Strategy / Ministry / Project filters (all filtering is done client-side in JS), and
    /// <see cref="GeoProjectItem"/> already carries both governorate and district codes, so one
    /// payload feeds either level without a round-trip when the user switches.
    /// </summary>
    public class GovernorateMapViewModel
    {
        public List<GovernorateRef> Governorates { get; set; } = new();
        public List<DistrictRef> Districts { get; set; } = new();
        public List<GeoProjectItem> Projects { get; set; } = new();
        public List<StrategyRef> Strategies { get; set; } = new();
        public List<MinistryRef> Ministries { get; set; } = new();
        public int TotalProjects { get; set; }

        /// <summary>
        /// Which level the map opens on: <c>governorate</c> (default) or <c>district</c>.
        /// /Reports/DistrictMap redirects here with this set, so old links still land correctly.
        /// </summary>
        public string Level { get; set; } = MapLevels.Governorate;

        public bool IsDistrictLevel =>
            string.Equals(Level, MapLevels.District, StringComparison.OrdinalIgnoreCase);
    }

    public static class MapLevels
    {
        public const string Governorate = "governorate";
        public const string District = "district";

        /// <summary>Anything unrecognised falls back to governorate rather than erroring.</summary>
        public static string Normalize(string? level) =>
            string.Equals(level, District, StringComparison.OrdinalIgnoreCase)
                ? District
                : Governorate;
    }

    public class GovernorateRef
    {
        public string Code { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
    }

    /// <summary>
    /// A district (ADM2). <see cref="Code"/> is the PCode used to join to the GeoJSON boundaries
    /// in wwwroot/geo/syr_admin2.json (feature property <c>adm2_pcode</c>).
    /// </summary>
    public class DistrictRef
    {
        public string Code { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string GovernorateCode { get; set; } = string.Empty;
    }

    public class StrategyRef
    {
        public int Code { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class MinistryRef
    {
        public int Code { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class GeoProjectItem
    {
        public int ProjectID { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public int? MinistryCode { get; set; }
        public string Ministry { get; set; } = string.Empty;
        public double EstimatedBudget { get; set; }
        public string Currency { get; set; } = "USD";
        public double Performance { get; set; }
        public double DisbursementPerformance { get; set; }
        public double TotalRealised { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public bool IsNational { get; set; }

        // Relationships used for client-side filtering / highlighting
        public List<int> FrameworkCodes { get; set; } = new();
        public List<string> GovernorateCodes { get; set; } = new();
        public List<string> DistrictCodes { get; set; } = new();
        public List<string> Communities { get; set; } = new();
    }
}
