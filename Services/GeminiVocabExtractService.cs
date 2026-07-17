using System.Net;
using System.Text;
using System.Text.Json;

namespace LexiLearn.Services
{
    public class GeminiVocabExtractService
    {
        private const string DefaultModel = "gemini-2.5-flash";
        private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

        private static readonly HashSet<string> SupportedImageTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp", "image/gif"
        };

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiVocabExtractService> _logger;

        public GeminiVocabExtractService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<GeminiVocabExtractService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Trích xuất từ vựng từ file ảnh hoặc PDF bằng Gemini Vision API.
        /// Synonym cũng sẽ được tạo thành entry riêng.
        /// </summary>
        public async Task<List<ExtractedVocabItem>> ExtractVocabularyAsync(
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            if (file.Length > MaxFileSize)
                throw new InvalidOperationException("File quá lớn. Giới hạn 10MB.");

            var mimeType = GetMimeType(file);
            if (mimeType == null)
                throw new InvalidOperationException("Định dạng file không được hỗ trợ. Chỉ hỗ trợ ảnh (JPG, PNG) và PDF.");

            var apiKey = _configuration["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Gemini API key chưa được cấu hình.");

            var model = _configuration["Gemini:Model"] ?? DefaultModel;
            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(apiKey)}";

            // Đọc file thành base64
            byte[] fileBytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms, cancellationToken);
                fileBytes = ms.ToArray();
            }
            var base64Data = Convert.ToBase64String(fileBytes);

            var request = CreateExtractionRequest(base64Data, mimeType);
            using var httpContent = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.PostAsync(endpoint, httpContent, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini vocab extraction failed with {StatusCode}: {Response}", response.StatusCode, responseText);
                throw new GeminiDictionaryException(response.StatusCode, "Gemini không thể đọc file. Vui lòng thử lại.");
            }

            var generatedJson = ExtractGeneratedText(responseText);
            if (string.IsNullOrWhiteSpace(generatedJson))
            {
                _logger.LogWarning("Gemini vocab extraction returned empty: {Response}", responseText);
                throw new GeminiDictionaryException(HttpStatusCode.UnprocessableEntity, "Không trích xuất được từ vựng từ file.");
            }

            var rawItems = JsonSerializer.Deserialize<List<ExtractedVocabRaw>>(
                generatedJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (rawItems == null || rawItems.Count == 0)
                throw new GeminiDictionaryException(HttpStatusCode.UnprocessableEntity, "Không tìm thấy từ vựng trong file.");

            // Flatten: tạo card chính + card cho mỗi synonym
            var result = new List<ExtractedVocabItem>();
            foreach (var raw in rawItems)
            {
                if (string.IsNullOrWhiteSpace(raw.Term) || string.IsNullOrWhiteSpace(raw.Meaning))
                    continue;

                // Card chính
                result.Add(new ExtractedVocabItem
                {
                    Term = raw.Term.Trim(),
                    Meaning = raw.Meaning.Trim(),
                    Ipa = raw.Ipa?.Trim(),
                    Example = raw.Example?.Trim()
                });

                // Card cho mỗi synonym
                if (raw.Synonyms != null)
                {
                    foreach (var syn in raw.Synonyms)
                    {
                        var cleanSyn = syn?.Trim();
                        if (string.IsNullOrWhiteSpace(cleanSyn)) continue;

                        // Bỏ qua nếu synonym thực chất là phiên âm (thường nằm trong /.../ hoặc [...])
                        if ((cleanSyn.StartsWith("/") && cleanSyn.EndsWith("/")) ||
                            (cleanSyn.StartsWith("[") && cleanSyn.EndsWith("]")))
                        {
                            continue;
                        }

                        // Xoá bỏ phiên âm dính kèm nếu AI trả về dạng "word /ipa/"
                        var ipaIndex = cleanSyn.IndexOf('/');
                        if (ipaIndex > 0)
                        {
                            cleanSyn = cleanSyn.Substring(0, ipaIndex).Trim();
                        }
                        if (string.IsNullOrWhiteSpace(cleanSyn)) continue;

                        result.Add(new ExtractedVocabItem
                        {
                            Term = cleanSyn,
                            Meaning = raw.Meaning.Trim(),
                            Ipa = null,
                            Example = null
                        });
                    }
                }
            }

            return result;
        }

        private static string? GetMimeType(IFormFile file)
        {
            var contentType = file.ContentType?.ToLowerInvariant();
            if (contentType == "application/pdf") return "application/pdf";
            if (contentType != null && SupportedImageTypes.Contains(contentType)) return contentType;

            // Fallback: kiểm tra extension
            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            return ext switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => null
            };
        }

        private static object CreateExtractionRequest(string base64Data, string mimeType)
        {
            var prompt = """
                You are a vocabulary extraction assistant for a language learning app.
                
                Look at this image/document carefully. It contains a vocabulary table or list.
                Extract ALL vocabulary entries from it.
                
                For each entry, extract:
                - term: the English word or phrase (the main glossary/vocabulary item)
                - meaning: the Vietnamese translation/meaning
                - ipa: the IPA pronunciation if available (e.g. /əˈkɔː.dɪŋ tuː/)
                - example: an example sentence if available
                - synonyms: an array of synonym words/phrases if shown in the table (e.g. "next to", "besides", "in place of").
                
                Important rules:
                - Extract every single row/entry, do not skip any
                - If a column is missing or empty, use null
                - For synonyms, ONLY extract the actual English words/phrases. DO NOT extract the IPA pronunciation of the synonyms. For example, if a synonym is shown as "next to /nekst ˌtuː/", the synonym is just "next to". Do not include "/nekst ˌtuː/" as a synonym.
                - Keep all text exactly as written in the source, but remove any IPA from the synonyms array.
                - Return a JSON array of objects
                """;

            return new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = mimeType,
                                    data = base64Data
                                }
                            },
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.1,
                    responseMimeType = "application/json",
                    responseSchema = new
                    {
                        type = "ARRAY",
                        items = new
                        {
                            type = "OBJECT",
                            properties = new Dictionary<string, object>
                            {
                                ["term"] = new { type = "STRING" },
                                ["meaning"] = new { type = "STRING" },
                                ["ipa"] = new { type = "STRING", nullable = true },
                                ["example"] = new { type = "STRING", nullable = true },
                                ["synonyms"] = new
                                {
                                    type = "ARRAY",
                                    items = new { type = "STRING" },
                                    nullable = true
                                }
                            },
                            required = new[] { "term", "meaning" }
                        }
                    }
                }
            };
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

    // DTO nội bộ — dùng để parse JSON từ Gemini
    public class ExtractedVocabRaw
    {
        public string Term { get; set; } = string.Empty;
        public string Meaning { get; set; } = string.Empty;
        public string? Ipa { get; set; }
        public string? Example { get; set; }
        public List<string>? Synonyms { get; set; }
    }

    // Kết quả cuối cùng (đã flatten synonym thành entry riêng)
    public class ExtractedVocabItem
    {
        public string Term { get; set; } = string.Empty;
        public string Meaning { get; set; } = string.Empty;
        public string? Ipa { get; set; }
        public string? Example { get; set; }
    }
}
