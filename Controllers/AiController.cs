using LexiLearn.Services;
using LexiLearn.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LexiLearn.Controllers
{
    [Authorize]
    [Route("api/ai")]
    public class AiController : Controller
    {
        private readonly GeminiDictionaryService _dictionaryService;
        private readonly GeminiChatService _chatService;

        public AiController(GeminiDictionaryService dictionaryService, GeminiChatService chatService)
        {
            _dictionaryService = dictionaryService;
            _chatService = chatService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        [HttpGet("dictionary")]
        public async Task<IActionResult> Dictionary(string term, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return BadRequest(new { message = "Vui lòng nhập từ cần tra." });
            }

            try
            {
                var result = await _dictionaryService.LookupAsync(term, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    message = "Chưa cấu hình Gemini API key. Hãy đặt biến môi trường GEMINI_API_KEY hoặc cấu hình Gemini:ApiKey."
                });
            }
            catch (GeminiDictionaryException ex)
            {
                return StatusCode((int)ex.StatusCode, new { message = "Gemini chưa trả được kết quả phù hợp. Vui lòng thử lại." });
            }
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> Conversations(CancellationToken cancellationToken)
        {
            var conversations = await _chatService.GetConversationsAsync(GetUserId(), cancellationToken);
            return Ok(conversations);
        }

        [HttpGet("conversations/{id:int}")]
        public async Task<IActionResult> Conversation(int id, CancellationToken cancellationToken)
        {
            var conversation = await _chatService.GetConversationAsync(GetUserId(), id, cancellationToken);
            if (conversation == null) return NotFound(new { message = "Không tìm thấy cuộc trò chuyện." });

            return Ok(conversation);
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] AiChatRequestViewModel? request, CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { message = "Vui lòng nhập nội dung cần hỏi." });
            }

            try
            {
                var result = await _chatService.SendAsync(GetUserId(), request.ConversationId, request.Message, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    message = "Chưa cấu hình Gemini API key. Hãy đặt biến môi trường GEMINI_API_KEY hoặc cấu hình Gemini:ApiKey."
                });
            }
            catch (GeminiDictionaryException ex)
            {
                return StatusCode((int)ex.StatusCode, new { message = "Gemini chưa trả lời được. Vui lòng thử lại." });
            }
        }

        [HttpDelete("conversations/{id:int}")]
        public async Task<IActionResult> DeleteConversation(int id, CancellationToken cancellationToken)
        {
            var deleted = await _chatService.DeleteConversationAsync(GetUserId(), id, cancellationToken);
            if (!deleted) return NotFound(new { message = "Không tìm thấy cuộc trò chuyện." });

            return Ok(new { success = true });
        }
    }
}
