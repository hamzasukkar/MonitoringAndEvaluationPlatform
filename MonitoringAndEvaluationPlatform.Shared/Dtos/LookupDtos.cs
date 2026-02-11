namespace MonitoringAndEvaluationPlatform.Dtos
{
    public class LookupItemDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? NameAr { get; set; }
        public string? NameEn { get; set; }
    }

    public class LookupItemIntDto
    {
        public int Code { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? NameAr { get; set; }
        public string? NameEn { get; set; }
    }
}
