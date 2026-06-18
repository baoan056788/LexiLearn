using LexiLearn.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LexiLearn.Controllers
{
    [Authorize]
    [Route("api/ai")]
    public class AiController : Controller
    {
        private readonly GeminiDictionaryService _dictionaryService;

        public AiController(GeminiDictionaryService dictionaryService)
        {
            _dictionaryService = dictionaryService;
        }

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
    }
}
