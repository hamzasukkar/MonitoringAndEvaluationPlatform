using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Services
{
    public interface IChatbotService
    {
        IAsyncEnumerable<string> GetStreamingResponseAsync(
            List<ChatMessageDto> messages,
            string locale,
            CancellationToken cancellationToken = default);
    }
}
