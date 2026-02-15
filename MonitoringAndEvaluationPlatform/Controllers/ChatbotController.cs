using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize]
    [Route("api/chatbot")]
    [ApiController]
    public class ChatbotController : ControllerBase
    {
        private readonly IChatbotService _chatbotService;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(IChatbotService chatbotService, ILogger<ChatbotController> logger)
        {
            _chatbotService = chatbotService;
            _logger = logger;
        }

        [HttpPost("stream")]
        [IgnoreAntiforgeryToken]
        public async Task StreamChat([FromBody] ChatRequestDto request)
        {
            Response.ContentType = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";

            try
            {
                await foreach (var token in _chatbotService.GetStreamingResponseAsync(
                    request.Messages, request.Locale, HttpContext.RequestAborted))
                {
                    var data = JsonSerializer.Serialize(new { token });
                    await Response.WriteAsync($"data: {data}\n\n", HttpContext.RequestAborted);
                    await Response.Body.FlushAsync(HttpContext.RequestAborted);
                }

                await Response.WriteAsync("data: [DONE]\n\n", HttpContext.RequestAborted);
                await Response.Body.FlushAsync(HttpContext.RequestAborted);
            }
            catch (OperationCanceledException)
            {
                // Client disconnected
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Gemini API request failed");
                var error = JsonSerializer.Serialize(new { token = "[Error: AI service unavailable]" });
                await Response.WriteAsync($"data: {error}\n\n");
                await Response.WriteAsync("data: [DONE]\n\n");
                await Response.Body.FlushAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chatbot streaming error");
                var error = JsonSerializer.Serialize(new { token = "[Error: " + ex.Message + "]" });
                await Response.WriteAsync($"data: {error}\n\n");
                await Response.WriteAsync("data: [DONE]\n\n");
                await Response.Body.FlushAsync();
            }
        }
    }
}
