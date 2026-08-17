using MonitoringAndEvaluationPlatform.ViewModels;

namespace MonitoringAndEvaluationPlatform.Services
{
    /// <summary>
    /// Builds and tears down complete experimental hierarchies
    /// (Framework → Outcome → Output → SubOutput → Indicator → Project → Phase → ActionPlan → Plan).
    /// Admin-only; every generated entity is name-prefixed so <see cref="DeleteByPrefixAsync"/>
    /// can remove exactly what was generated and nothing else.
    /// </summary>
    public interface ITestDataGeneratorService
    {
        /// <summary>
        /// Creates one framework and its full subtree under the configured ministry.
        /// Throws <see cref="InvalidOperationException"/> if the ministry is missing or the
        /// estimated row count exceeds <see cref="GenerateTestDataViewModel.MaxRowCount"/>.
        /// </summary>
        Task<TestDataGenerationResult> GenerateAsync(GenerateTestDataViewModel config);

        /// <summary>
        /// Deletes every framework and project whose name starts with <paramref name="prefix"/>,
        /// along with the whole subtree below them.
        /// </summary>
        Task<TestDataGenerationResult> DeleteByPrefixAsync(string prefix);
    }
}
