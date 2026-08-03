namespace MonitoringAndEvaluationPlatform.Models
{
    public interface IHasTimestamps
    {
        DateTime CreatedAt { get; set; }
        DateTime LastModifiedAt { get; set; }
    }
}
