using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Services
{
    public class ChatbotService : IChatbotService
    {
        private readonly string _apiKey;
        private readonly string _modelName;
        private readonly IHttpClientFactory _httpClientFactory;

        private const string SystemPrompt = @"You are a helpful AI assistant for the Monitoring and Evaluation (M&E) Platform. This platform is used to monitor and evaluate development projects with a hierarchical performance tracking system.

## Platform Hierarchy
Framework > Outcome > Output > SubOutput > Indicator > Project > ActionPlan > Activity > Plan

## Main Modules
1. **Results Framework** (/Frameworks) - Manage the hierarchical results framework (Frameworks, Outcomes, Outputs, SubOutputs, Indicators)
2. **Set Up** (/Ministries) - Configure ministries, donors, sectors, and other reference data
3. **Projects** (/Projects) - Manage development projects, link them to indicators, assign locations/sectors/donors
4. **Monitoring** (/Monitoring) - Track project implementation through plans (Financial, Physical, Disbursement Performance, Field Monitoring, Impact Assessment)
5. **Dashboard** (/Dashboard) - Visual analytics and performance charts
6. **Admin** (/Admin) - User management and system administration (System Administrators only)
7. **User Guide** (/Home/UserGuide) - Platform documentation and help

## Performance Calculation
- Plans track planned vs realized values for activities
- Project performance is calculated from activity plans by type
- Performance propagates upward: Project > Indicator > SubOutput > Output > Outcome > Framework

## Key Features
- Multi-language support (Arabic, English, French)
- Theme customization (Default, Calm Blue, Alternative, Modern Glass)
- Role-based access control (System Administrator, Ministries User, Data Entry, Reading User)
- Location hierarchy (Governorate > District > SubDistrict > Community)
- Export capabilities (Excel, PDF)
- Notification system for monitoring updates

## Navigation Tips
- Use the sidebar to navigate between main modules
- Use breadcrumbs to navigate within the results framework hierarchy
- The back button returns to the previous page
- Language can be changed from the settings

Answer user questions about the platform clearly and concisely. Help them navigate, understand features, and troubleshoot issues.";

        public ChatbotService(IOptions<ChatbotSettings> settings, IHttpClientFactory httpClientFactory)
        {
            _apiKey = settings.Value.ApiKey;
            _modelName = settings.Value.ModelName;
            _httpClientFactory = httpClientFactory;
        }

        public async IAsyncEnumerable<string> GetStreamingResponseAsync(
            List<ChatMessageDto> messages,
            string locale,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var localeInstruction = locale switch
            {
                "ar" => "Always respond in Arabic (العربية). Use right-to-left text.",
                "fr" => "Always respond in French (Français).",
                _ => "Always respond in English."
            };

            // Build OpenAI-compatible request body (works with Groq)
            var chatMessages = new List<object>
            {
                new { role = "system", content = SystemPrompt + "\n\n" + localeInstruction }
            };

            // The role is clamped to user/assistant and the history is bounded. Previously
            // msg.Role was passed through verbatim, so a client could inject its own
            // role:"system" message after the platform prompt and override the instructions
            // above - and send an unbounded history to a metered API.
            const int maxMessages = 20;
            const int maxContentChars = 4000;

            foreach (var msg in messages.TakeLast(maxMessages))
            {
                if (string.IsNullOrWhiteSpace(msg.Content))
                {
                    continue;
                }

                var role = msg.Role == "assistant" ? "assistant" : "user";
                var messageText = msg.Content.Length > maxContentChars
                    ? msg.Content[..maxContentChars]
                    : msg.Content;

                chatMessages.Add(new { role, content = messageText });
            }

            var requestBody = new
            {
                model = _modelName,
                messages = chatMessages,
                temperature = 0.7,
                max_tokens = 2048,
                stream = true
            };

            var client = _httpClientFactory.CreateClient();
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            request.Content = content;

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrEmpty(line) || !line.StartsWith("data: "))
                    continue;

                var data = line.Substring(6).Trim();
                if (data == "[DONE]")
                    break;

                using var doc = JsonDocument.Parse(data);
                var root = doc.RootElement;

                if (root.TryGetProperty("choices", out var choices))
                {
                    foreach (var choice in choices.EnumerateArray())
                    {
                        if (choice.TryGetProperty("delta", out var delta) &&
                            delta.TryGetProperty("content", out var tokenEl))
                        {
                            var token = tokenEl.GetString();
                            if (!string.IsNullOrEmpty(token))
                                yield return token;
                        }
                    }
                }
            }
        }
    }
}
