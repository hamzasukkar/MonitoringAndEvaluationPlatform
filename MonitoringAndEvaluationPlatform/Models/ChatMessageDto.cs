using System.ComponentModel.DataAnnotations;

namespace MonitoringAndEvaluationPlatform.Models
{
    public class ChatMessageDto
    {
        [Required]
        public string Role { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;
    }

    public class ChatRequestDto
    {
        [Required]
        public List<ChatMessageDto> Messages { get; set; } = new();

        public string Locale { get; set; } = "ar";
    }
}
