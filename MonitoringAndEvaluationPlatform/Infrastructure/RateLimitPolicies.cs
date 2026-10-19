namespace MonitoringAndEvaluationPlatform.Infrastructure
{
    /// <summary>
    /// Names of the rate-limiting policies registered in Program.cs.
    /// Referenced from [EnableRateLimiting] attributes and endpoint conventions.
    /// </summary>
    public static class RateLimitPolicies
    {
        /// <summary>Credential-guessing protection on the sign-in path. Partitioned by client IP.</summary>
        public const string Login = "login";

        /// <summary>Protects the metered LLM API quota. Partitioned by user.</summary>
        public const string Chatbot = "chatbot";

        /// <summary>Destructive admin operations guarded by step-up re-authentication.</summary>
        public const string Sensitive = "sensitive";
    }
}
