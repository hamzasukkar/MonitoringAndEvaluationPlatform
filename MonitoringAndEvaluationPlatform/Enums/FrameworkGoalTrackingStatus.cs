namespace MonitoringAndEvaluationPlatform.Enums
{
    /// <summary>
    /// Whether a goal's actual progress is keeping pace with the time elapsed toward its target.
    /// Single source of truth for "on track" classification — see FrameworkGoal.TrackingStatus.
    /// </summary>
    public enum FrameworkGoalTrackingStatus
    {
        OnTrack,
        AtRisk,
        OffTrack
    }
}
