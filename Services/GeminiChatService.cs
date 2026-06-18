using System.Net;
using System.Text;
using System.Text.Json;
using LexiLearn.Data;
using LexiLearn.Models;
using LexiLearn.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LexiLearn.Services
{
    public class GeminiChatService
    {
        private const string DefaultModel = "gemini-2.5-flash";
        private const string SystemPrompt = """
            Bạn là trợ lý học từ vựng tiếng Anh cho người Việt trong ứng dụng LexiLearn.
            Trả lời bằng tiếng Việt, rõ ràng, ngắn gọn và có cấu trúc.
            Khi giải thích từ vựng, ưu tiên: nghĩa tiếng Việt, IPA nếu biết, ví dụ tiếng Anh, dịch ví dụ, lỗi thường gặp.
            Khi người dùng hỏi phân biệt từ, hãy nêu khác biệt ngữ cảnh dùng và ví dụ.
            Khi người dùng muốn tạo bộ từ, hãy trả về danh sách gồm term, meaning, ipa, example.
            Không bịa nếu không chắc; hãy nói cần thêm ngữ cảnh.
            """;

        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiChatService> _logger;

        public GeminiChatService(
            AppDbContext context,
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<GeminiChatService> logger)
        {
            _context = context;
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AiChatResponseViewModel> SendAsync(int userId, int? conversationId, string message, CancellationToken cancellationToken = default)
        {
            var normalizedMessage = message.Trim();
            if (string.IsNullOrWhiteSpace(normalizedMessage))
            {
                throw new ArgumentException("Message cannot be empty.", nameof(message));
            }

            var conversation = await GetOrCreateConversationAsync(userId, conversationId, normalizedMessage, cancellationToken);
            var recentMessages = await _context.AiMessages
                .AsNoTracking()
                .Where(m => m.AiConversationId == conversation.AiConversationId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(12)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync(cancellationToken);

            var reply = await GenerateReplyAsync(recentMessages, normalizedMessage, cancellationToken);

            _context.AiMessages.Add(new AiMessage
            {
                AiConversationId = conversation.AiConversationId,
                Role = "user",
                Content = normalizedMessage,
                CreatedAt = DateTime.Now
            });

            _context.AiMessages.Add(new AiMessage
            {
                AiConversationId = conversation.AiConversationId,
                Role = "assistant",
                Content = reply,
                CreatedAt = DateTime.Now
            });

            conversation.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync(cancellationToken);

            return await BuildResponseAsync(conversation.AiConversationId, reply, cancellationToken);
        }

        public async Task<List<AiConversationSummaryViewModel>> GetConversationsAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _context.AiConversations
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .Take(20)
                .Select(c => new AiConversationSummaryViewModel
                {
                    ConversationId = c.AiConversationId,
                    Title = c.Title,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<AiChatResponseViewModel?> GetConversationAsync(int userId, int conversationId, CancellationToken cancellationToken = default)
        {
            var conversation = await _context.AiConversations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.AiConversationId == conversationId && c.UserId == userId, cancellationToken);

            if (conversation == null)
            {
                return null;
            }

            return await BuildResponseAsync(conversationId, string.Empty, cancellationToken);
        }

        public async Task<bool> DeleteConversationAsync(int userId, int conversationId, CancellationToken cancellationToken = default)
        {
            var conversation = await _context.AiConversations
                .FirstOrDefaultAsync(c => c.AiConversationId == conversationId && c.UserId == userId, cancellationToken);

            if (conversation == null) return false;

            _context.AiConversations.Remove(conversation);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private async Task<AiConversation> GetOrCreateConversationAsync(int userId, int? conversationId, string firstMessage, CancellationToken cancellationToken)
        {
            if (conversationId.HasValue)
            {
                var existing = await _context.AiConversations
                    .FirstOrDefaultAsync(c => c.AiConversationId == conversationId.Value && c.UserId == userId, cancellationToken);

                if (existing != null)
                {
                    return existing;
                }
            }

            var title = firstMessage.Length > 60 ? firstMessage[..60] + "..." : firstMessage;
            var conversation = new AiConversation
            {
                UserId = userId,
                Title = title,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.AiConversations.Add(conversation);
            await _context.SaveChangesAsync(cancellationToken);
            return conversation;
        }

        private async Task<string> GenerateReplyAsync(List<AiMessage> recentMessages, string userMessage, CancellationToken cancellationToken)
        {
            var apiKey = _configuration["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("Gemini API key is not configured.");
            }

            var model = _configuration["Gemini:Model"] ?? DefaultModel;
            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(apiKey)}";
            var contents = BuildContents(recentMessages, userMessage);
            using var content = new StringContent(JsonSerializer.Serialize(contents), Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini chat failed with {StatusCode}: {Response}", response.StatusCode, responseText);
                throw new GeminiDictionaryException(response.StatusCode, "Gemini chat request failed.");
            }

            return ExtractGeneratedText(responseText).Trim();
        }

        private static object BuildContents(List<AiMessage> recentMessages, string userMessage)
        {
            var contents = new List<object>
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = SystemPrompt } }
                },
                new
                {
                    role = "model",
                    parts = new[] { new { text = "Mình đã sẵn sàng hỗ trợ học từ vựng tiếng Anh trong LexiLearn." } }
                }
            };

            foreach (var message in recentMessages)
            {
                contents.Add(new
                {
                    role = message.Role == "assistant" ? "model" : "user",
                    parts = new[] { new { text = message.Content } }
                });
            }

            contents.Add(new
            {
                role = "user",
                parts = new[] { new { text = userMessage } }
            });

            return new
            {
                contents,
                generationConfig = new
                {
                    temperature = 0.4,
                    maxOutputTokens = 900
                }
            };
        }

        private async Task<AiChatResponseViewModel> BuildResponseAsync(int conversationId, string reply, CancellationToken cancellationToken)
        {
            var conversation = await _context.AiConversations
                .AsNoTracking()
                .FirstAsync(c => c.AiConversationId == conversationId, cancellationToken);

            var messages = await _context.AiMessages
                .AsNoTracking()
                .Where(m => m.AiConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new AiChatMessageViewModel
                {
                    Role = m.Role,
                    Content = m.Content,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return new AiChatResponseViewModel
            {
                ConversationId = conversation.AiConversationId,
                Title = conversation.Title,
                Reply = reply,
                Messages = messages
            };
        }

        private static string ExtractGeneratedText(string responseText)
        {
            using var document = JsonDocument.Parse(responseText);
            var parts = document.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts");

            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var textElement))
                {
                    return textElement.GetString() ?? "";
                }
            }

            return "";
        }
    }
}
