namespace MonitoringAndEvaluationPlatform.ViewModel
{
    // ViewModel for the District Map report (Reports/DistrictMap).
    // Same shape as GovernorateMapViewModel but at district (ADM2) level;
    // districts are matched to the GeoJSON boundaries by PCode (District.Code).
    public class DistrictMapViewModel
    {
        public List<DistrictRef> Districts { get; set; } = new();
        public List<GovernorateRef> Governorates { get; set; } = new();
        public List<GeoProjectItem> Projects { get; set; } = new();
        public List<StrategyRef> Strategies { get; set; } = new();
        public List<MinistryRef> Ministries { get; set; } = new();
        public int TotalProjects { get; set; }
    }

    public class DistrictRef
    {
        public string Code { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string GovernorateCode { get; set; } = string.Empty;
    }
}
