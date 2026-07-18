using System.Net;
using System.Text;
using System.Text.Json;
using LexiLearn.ViewModels;

namespace LexiLearn.Services
{
    public class GeminiDictionaryService
    {
        private const string DefaultModel = "gemini-2.0-flash";
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiDictionaryService> _logger;

        public GeminiDictionaryService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<GeminiDictionaryService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AiDictionaryViewModel> LookupAsync(string term, CancellationToken cancellationToken = default)
        {
            var apiKey = _configuration["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("Gemini API key is not configured.");
            }

            var model = _configuration["Gemini:Model"] ?? DefaultModel;
            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(apiKey)}";
            var request = CreateRequest(term);
            using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini dictionary lookup failed with {StatusCode}: {Response}", response.StatusCode, responseText);
                throw new GeminiDictionaryException(response.StatusCode, "Gemini API request failed.");
            }

            var generatedJson = ExtractGeneratedText(responseText);
            if (string.IsNullOrWhiteSpace(generatedJson))
            {
                _logger.LogWarning("Gemini dictionary returned an empty payload: {Response}", responseText);
                throw new GeminiDictionaryException(HttpStatusCode.UnprocessableEntity, "Gemini returned an empty dictionary result.");
            }

            var result = JsonSerializer.Deserialize<AiDictionaryViewModel>(
                generatedJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result == null || string.IsNullOrWhiteSpace(result.Word))
            {
                throw new GeminiDictionaryException(HttpStatusCode.UnprocessableEntity, "Gemini returned an invalid dictionary result.");
            }

            result.Word = result.Word.Trim();
            result.Ipa = (result.Ipa ?? string.Empty).Trim();
            result.VietnameseMeaning = (result.VietnameseMeaning ?? string.Empty).Trim();
            result.PartOfSpeech = (result.PartOfSpeech ?? string.Empty).Trim();
            result.Note = (result.Note ?? string.Empty).Trim();
            result.Definitions = (result.Definitions ?? new List<AiDictionaryDefinitionViewModel>())
                .Where(d => !string.IsNullOrWhiteSpace(d.Definition))
                .Take(4)
                .ToList();
            result.Synonyms = (result.Synonyms ?? new List<string>()).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).Take(8).ToList();
            result.Antonyms = (result.Antonyms ?? new List<string>()).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).Take(8).ToList();

            return result;
        }

        private static object CreateRequest(string term)
        {
            var prompt = $"""
                You are a concise English-Vietnamese dictionary for a vocabulary learning web app.
                Look up this term or phrase: "{term.Trim()}".

                If the user input is Vietnamese, translate it to the most natural English headword first.
                Return exactly one JSON object matching the schema. Do not include markdown.
                Keep Vietnamese text natural and short. Use semicolons to separate Vietnamese meanings.
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
                    responseMimeType = "application/json",
                    responseSchema = new
                    {
                        type = "OBJECT",
                        properties = new Dictionary<string, object>
                        {
                            ["word"] = new { type = "STRING" },
                            ["ipa"] = new { type = "STRING" },
                            ["vietnameseMeaning"] = new { type = "STRING" },
                            ["partOfSpeech"] = new { type = "STRING" },
                            ["definitions"] = new
                            {
                                type = "ARRAY",
                                items = new
                                {
                                    type = "OBJECT",
                                    properties = new Dictionary<string, object>
                                    {
                                        ["definition"] = new { type = "STRING" },
                                        ["example"] = new { type = "STRING" },
                                        ["exampleVi"] = new { type = "STRING" }
                                    },
                                    required = new[] { "definition", "example", "exampleVi" }
                                }
                            },
                            ["synonyms"] = new { type = "ARRAY", items = new { type = "STRING" } },
                            ["antonyms"] = new { type = "ARRAY", items = new { type = "STRING" } },
                            ["note"] = new { type = "STRING" }
                        },
                        required = new[] { "word", "ipa", "vietnameseMeaning", "partOfSpeech", "definitions", "synonyms", "antonyms", "note" }
                    }
                }
            };
        }

        public async Task<List<string>> GenerateVietnameseSynonymsAsync(string meaning, CancellationToken cancellationToken = default)
        {
            var apiKey = _configuration["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Gemini API key is not configured.");

            var model = _configuration["Gemini:Model"] ?? DefaultModel;
            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(apiKey)}";
            
            var prompt = $"Liệt kê 3 từ đồng nghĩa phổ biến trong tiếng Việt của cụm từ: '{meaning}'. Chỉ trả lời bằng mảng JSON chứa các chuỗi, không có text nào khác. Ví dụ: [\"từ 1\", \"từ 2\"]";
            
            var request = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
                generationConfig = new { temperature = 0.2, responseMimeType = "application/json" }
            };

            using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new List<string>();

            var generatedJson = ExtractGeneratedText(responseText);
            if (string.IsNullOrWhiteSpace(generatedJson)) return new List<string>();

            try
            {
                var list = JsonSerializer.Deserialize<List<string>>(generatedJson);
                return list ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static string ExtractGeneratedText(string responseText)
        {
            using var document = JsonDocument.Parse(responseText);
            if (!document.RootElement.TryGetProperty("candidates", out var candidates) ||
                candidates.ValueKind != JsonValueKind.Array ||
                candidates.GetArrayLength() == 0)
            {
                return string.Empty;
            }

            foreach (var candidate in candidates.EnumerateArray())
            {
                if (!candidate.TryGetProperty("content", out var content) ||
                    !content.TryGetProperty("parts", out var parts) ||
                    parts.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var textElement))
                    {
                        var text = textElement.GetString();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            return text;
                        }
                    }
                }
            }

            return string.Empty;
        }
    }

    public class GeminiDictionaryException : Exception
    {
        public GeminiDictionaryException(HttpStatusCode statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }

        public HttpStatusCode StatusCode { get; }
    }
}
