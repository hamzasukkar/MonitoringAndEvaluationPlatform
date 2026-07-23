namespace MonitoringAndEvaluationPlatform.Enums
{
    public enum CommentKind
    {
        /// <summary>A general comment or a reply to another comment.</summary>
        Comment = 1,

        /// <summary>Tester tried the feature and found no issues.</summary>
        TestPassed = 2,

        /// <summary>Tester tried the feature and recorded notes/issues.</summary>
        TestHasNotes = 3
    }
}
