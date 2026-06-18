namespace LexiLearn.ViewModels
{
    public class AiChatRequestViewModel
    {
        public int? ConversationId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class AiChatResponseViewModel
    {
        public int ConversationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Reply { get; set; } = string.Empty;
        public List<AiChatMessageViewModel> Messages { get; set; } = new();
    }

    public class AiChatMessageViewModel
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class AiConversationSummaryViewModel
    {
        public int ConversationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }
}
