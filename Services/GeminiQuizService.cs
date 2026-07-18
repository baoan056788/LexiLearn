using System.Net;
using System.Text;
using System.Text.Json;
using LexiLearn.Models;

namespace LexiLearn.Services
{
    public class AiQuizEvaluationResult
    {
        public string CorrectAnswer { get; set; } = string.Empty;
        public string ExplanationHtml { get; set; } = string.Empty;
    }

    public class GeminiQuizService
    {
        private const string DefaultModel = "gemini-flash-latest";
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiQuizService> _logger;

        public GeminiQuizService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<GeminiQuizService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AiQuizEvaluationResult> EvaluateQuizAsync(LectureQuiz quiz, CancellationToken cancellationToken = default)
        {
            var apiKey = _configuration["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("Gemini API key is not configured.");
            }

            var model = _configuration["Gemini:Model"] ?? DefaultModel;
            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(apiKey)}";
            var request = CreateRequest(quiz);
            using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini quiz evaluation failed with {StatusCode}: {Response}", response.StatusCode, responseText);
                
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests || response.StatusCode == (System.Net.HttpStatusCode)429)
                {
                    throw new Exception("Hệ thống AI đang quá tải do có nhiều người sử dụng. Vui lòng đợi khoảng 1 phút rồi thử lại nhé!");
                }
                
                throw new Exception("Gemini API request failed. Status: " + response.StatusCode + ". Response: " + responseText);
            }

            var generatedJson = ExtractGeneratedText(responseText);
            if (string.IsNullOrWhiteSpace(generatedJson))
            {
                _logger.LogWarning("Gemini quiz returned an empty payload: {Response}", responseText);
                throw new Exception("Gemini returned an empty result.");
            }

            // Cleanup potential markdown if responseMimeType fails
            generatedJson = generatedJson.Trim();
            if (generatedJson.StartsWith("```json"))
            {
                generatedJson = generatedJson.Substring(7);
            }
            if (generatedJson.StartsWith("```"))
            {
                generatedJson = generatedJson.Substring(3);
            }
            if (generatedJson.EndsWith("```"))
            {
                generatedJson = generatedJson.Substring(0, generatedJson.Length - 3);
            }
            generatedJson = generatedJson.Trim();

            AiQuizEvaluationResult result;
            try
            {
                result = JsonSerializer.Deserialize<AiQuizEvaluationResult>(
                    generatedJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse JSON from Gemini. Raw text: {RawText}", generatedJson);
                throw new Exception($"Gemini returned malformed JSON. ({ex.Message}) Raw text length: {generatedJson.Length}");
            }

            if (result == null || string.IsNullOrWhiteSpace(result.CorrectAnswer))
            {
                throw new Exception("Gemini returned an invalid quiz result.");
            }

            return result;
        }

        private static object CreateRequest(LectureQuiz quiz)
        {
            var prompt = $$"""
                You are an expert English teacher. Evaluate the following multiple-choice question.
                Question: {{quiz.QuestionText}}
                A. {{quiz.OptionA}}
                B. {{quiz.OptionB}}
                C. {{quiz.OptionC}}
                D. {{quiz.OptionD}}

                Determine the correct answer (A, B, C, or D).
                Write a detailed explanation in Vietnamese formatted as HTML. Do NOT include ```html markdown blocks.
                The HTML should contain:
                1. A brief explanation of why the answer is correct and why the others are wrong.
                2. Dấu hiệu nhận biết (Grammar signs/keywords) that lead to the answer.
                3. Dịch nghĩa (Translation of the question and correct sentence).
                Use simple HTML tags like <p>, <strong>, <ul>, <li>, <br/>.
                Return exactly one JSON object matching this schema:
                {
                  "correctAnswer": "A",
                  "explanationHtml": "<p>...</p>"
                }
                """;

            return new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.2,
                    maxOutputTokens = 8192,
                    responseMimeType = "application/json"
                }
            };
        }

        private static string ExtractGeneratedText(string jsonResponse)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonResponse);
                var root = doc.RootElement;

                if (root.TryGetProperty("candidates", out var candidates) &&
                    candidates.ValueKind == JsonValueKind.Array &&
                    candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var content) &&
                        content.TryGetProperty("parts", out var parts) &&
                        parts.ValueKind == JsonValueKind.Array &&
                        parts.GetArrayLength() > 0)
                    {
                        var firstPart = parts[0];
                        if (firstPart.TryGetProperty("text", out var text))
                        {
                            return text.GetString() ?? string.Empty;
                        }
                    }
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
