using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using LexiLearn.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace LexiLearn.Services
{
    public class WordParserService
    {
        private readonly IWebHostEnvironment _env;

        public WordParserService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public class ParseResult
        {
            public string HtmlContent { get; set; } = string.Empty;
            public List<SectionInfo> Sections { get; set; } = new();
            public List<LectureQuiz> Quizzes { get; set; } = new();
        }

        public class SectionInfo
        {
            public string Title { get; set; } = string.Empty;
            public string AnchorId { get; set; } = string.Empty;
            public int HeadingLevel { get; set; } = 1;
            public int SortOrder { get; set; }
            public string HtmlContent { get; set; } = string.Empty;
        }

        public async Task<ParseResult> ParseAsync(IFormFile file, int lectureId)
        {
            var result = new ParseResult();
            var html = new StringBuilder();
            var sections = new List<SectionInfo>();
            var quizzes = new List<LectureQuiz>();
            int sectionIndex = 0;

            // Save uploaded images directory
            var imageDir = Path.Combine(_env.WebRootPath, "uploads", "lectures", lectureId.ToString());
            if (!Directory.Exists(imageDir))
                Directory.CreateDirectory(imageDir);

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var doc = WordprocessingDocument.Open(stream, false);
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body == null)
            {
                result.HtmlContent = "<p>Khong the doc noi dung file Word.</p>";
                return result;
            }

            // Process images
            var imageMap = new Dictionary<string, string>();
            if (doc.MainDocumentPart != null)
            {
                foreach (var imagePart in doc.MainDocumentPart.ImageParts)
                {
                    var rid = doc.MainDocumentPart.GetIdOfPart(imagePart);
                    var ext = imagePart.ContentType switch
                    {
                        "image/png" => ".png",
                        "image/jpeg" => ".jpg",
                        "image/gif" => ".gif",
                        "image/bmp" => ".bmp",
                        "image/svg+xml" => ".svg",
                        _ => ".png"
                    };
                    var fileName = $"img_{rid}{ext}";
                    var filePath = Path.Combine(imageDir, fileName);

                    using (var imgStream = imagePart.GetStream())
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imgStream.CopyToAsync(fileStream);
                    }

                    imageMap[rid] = $"/uploads/lectures/{lectureId}/{fileName}";
                }
            }

            // Track quiz detection state
            var quizBuffer = new StringBuilder();
            string? currentQuestion = null;
            string? optA = null, optB = null, optC = null, optD = null;
            int currentQuizSectionId = 0;

            foreach (var element in body.Elements())
            {
                if (element is Paragraph para)
                {
                    var styleId = para.ParagraphProperties?.ParagraphStyleId?.Val?.Value ?? "";
                    var text = GetParagraphText(para);

                    // Check for headings
                    int headingLevel = GetHeadingLevel(styleId, para);

                    if (headingLevel > 0 && !string.IsNullOrWhiteSpace(text))
                    {
                        sectionIndex++;
                        var anchorId = $"section-{sectionIndex}";
                        sections.Add(new SectionInfo
                        {
                            Title = text.Trim(),
                            AnchorId = anchorId,
                            HeadingLevel = headingLevel,
                            SortOrder = sectionIndex
                        });

                        var headingHtml = $"<h{headingLevel} id=\"{anchorId}\" class=\"lecture-heading\">{FormatRunsToHtml(para, imageMap)}</h{headingLevel}>\n";
                        html.AppendLine(headingHtml);
                        sections.Last().HtmlContent += headingHtml;
                        continue;
                    }

                    // Check for quiz questions
                    var trimmedText = text.Trim();
                    bool isQuizLine = TryDetectQuiz(trimmedText, ref currentQuestion, ref optA, ref optB, ref optC, ref optD, ref currentQuizSectionId, quizzes, sectionIndex);

                    var paragraphHtmlBuilder = new StringBuilder();

                    // Check for images in paragraph
                    var hasImage = para.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().Any();

                    if (hasImage)
                    {
                        paragraphHtmlBuilder.AppendLine("<div class=\"lecture-image-container\">");
                        foreach (var blip in para.Descendants<DocumentFormat.OpenXml.Drawing.Blip>())
                        {
                            var embed = blip.Embed?.Value;
                            if (embed != null && imageMap.TryGetValue(embed, out var imgUrl))
                            {
                                paragraphHtmlBuilder.AppendLine($"<img src=\"{imgUrl}\" class=\"lecture-image\" alt=\"Hinh anh bai giang\" loading=\"lazy\" />");
                            }
                        }
                        paragraphHtmlBuilder.AppendLine("</div>");
                    }

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var formattedHtml = FormatRunsToHtml(para, imageMap);
                        // Check if it's a list item
                        var numPr = para.ParagraphProperties?.NumberingProperties;
                        if (numPr != null)
                        {
                            paragraphHtmlBuilder.AppendLine($"<li class=\"lecture-list-item\">{formattedHtml}</li>");
                        }
                        else
                        {
                            paragraphHtmlBuilder.AppendLine($"<p class=\"lecture-paragraph\">{formattedHtml}</p>");
                        }
                    }

                    if (paragraphHtmlBuilder.Length > 0)
                    {
                        var pHtml = paragraphHtmlBuilder.ToString();
                        html.Append(pHtml);
                        if (!isQuizLine && sections.Count > 0)
                        {
                            sections.Last().HtmlContent += pHtml;
                        }
                    }
                }
                else if (element is Table table)
                {
                    var tableHtml = ConvertTableToHtml(table, imageMap) + "\n";
                    html.Append(tableHtml);
                    if (sections.Count > 0)
                    {
                        sections.Last().HtmlContent += tableHtml;
                    }
                }
            }

            // Flush any remaining quiz
            FlushQuiz(ref currentQuestion, ref optA, ref optB, ref optC, ref optD, quizzes, currentQuizSectionId);

            result.HtmlContent = html.ToString();
            result.Sections = sections;
            result.Quizzes = quizzes;
            return result;
        }

        private string GetParagraphText(Paragraph para)
        {
            var sb = new StringBuilder();
            foreach (var run in para.Elements<Run>())
            {
                foreach (var text in run.Elements<Text>())
                {
                    sb.Append(text.Text);
                }
            }
            return sb.ToString();
        }

        private int GetHeadingLevel(string styleId, Paragraph para)
        {
            // Check style-based headings
            if (styleId.StartsWith("Heading", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(styleId.Replace("Heading", ""), out int level))
                    return Math.Min(level, 6);
            }

            // Check outline level
            var outlineLevel = para.ParagraphProperties?.OutlineLevel?.Val;
            if (outlineLevel != null)
            {
                return Math.Min(outlineLevel.Value + 1, 6);
            }

            return 0;
        }

        private string FormatRunsToHtml(Paragraph para, Dictionary<string, string> imageMap)
        {
            var sb = new StringBuilder();
            foreach (var run in para.Elements<Run>())
            {
                var text = string.Join("", run.Elements<Text>().Select(t => t.Text));
                if (string.IsNullOrEmpty(text))
                {
                    // Check for images in runs
                    foreach (var blip in run.Descendants<DocumentFormat.OpenXml.Drawing.Blip>())
                    {
                        var embed = blip.Embed?.Value;
                        if (embed != null && imageMap.TryGetValue(embed, out var imgUrl))
                        {
                            sb.Append($"<img src=\"{imgUrl}\" class=\"lecture-inline-image\" alt=\"\" loading=\"lazy\" />");
                        }
                    }
                    continue;
                }

                var rp = run.RunProperties;
                var styles = new List<string>();
                var openTags = new StringBuilder();
                var closeTags = new StringBuilder();

                if (rp != null)
                {
                    if (rp.Bold != null && (rp.Bold.Val == null || rp.Bold.Val.Value))
                    {
                        openTags.Append("<strong>");
                        closeTags.Insert(0, "</strong>");
                    }
                    if (rp.Italic != null && (rp.Italic.Val == null || rp.Italic.Val.Value))
                    {
                        openTags.Append("<em>");
                        closeTags.Insert(0, "</em>");
                    }
                    if (rp.Underline != null && rp.Underline.Val != null && rp.Underline.Val.Value != UnderlineValues.None)
                    {
                        openTags.Append("<u>");
                        closeTags.Insert(0, "</u>");
                    }
                    if (rp.Strike != null && (rp.Strike.Val == null || rp.Strike.Val.Value))
                    {
                        openTags.Append("<s>");
                        closeTags.Insert(0, "</s>");
                    }

                    // Font color
                    var color = rp.Color?.Val?.Value;
                    if (!string.IsNullOrEmpty(color) && color != "000000" && color != "auto")
                    {
                        styles.Add($"color:#{color}");
                    }

                    // Highlight/background
                    var highlight = rp.Highlight?.Val;
                    if (highlight != null && highlight.Value != HighlightColorValues.None)
                    {
                        var bgColor = GetHighlightColor(highlight.Value);
                        if (!string.IsNullOrEmpty(bgColor))
                        {
                            styles.Add($"background-color:{bgColor}");
                        }
                    }

                    // Font size
                    var fontSize = rp.FontSize?.Val?.Value;
                    if (!string.IsNullOrEmpty(fontSize) && int.TryParse(fontSize, out int halfPts))
                    {
                        var pts = halfPts / 2.0;
                        if (pts != 11 && pts != 12) // Skip default sizes
                        {
                            styles.Add($"font-size:{pts}pt");
                        }
                    }
                }

                var encodedText = System.Net.WebUtility.HtmlEncode(text);

                if (styles.Count > 0)
                {
                    sb.Append($"<span style=\"{string.Join(";", styles)}\">{openTags}{encodedText}{closeTags}</span>");
                }
                else if (openTags.Length > 0)
                {
                    sb.Append($"{openTags}{encodedText}{closeTags}");
                }
                else
                {
                    sb.Append(encodedText);
                }
            }
            return sb.ToString();
        }

        private string GetHighlightColor(HighlightColorValues color)
        {
            var colorStr = color.ToString();
            return colorStr switch
            {
                "yellow" => "#FFFF00",
                "green" => "#00FF00",
                "cyan" => "#00FFFF",
                "magenta" => "#FF00FF",
                "blue" => "#0000FF",
                "red" => "#FF0000",
                "darkBlue" => "#000080",
                "darkCyan" => "#008080",
                "darkGreen" => "#008000",
                "darkMagenta" => "#800080",
                "darkRed" => "#800000",
                "darkYellow" => "#808000",
                "lightGray" => "#C0C0C0",
                "darkGray" => "#808080",
                _ => ""
            };
        }

        private string ConvertTableToHtml(Table table, Dictionary<string, string> imageMap)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<div class=\"table-responsive\"><table class=\"table table-bordered lecture-table\">");

            bool isFirstRow = true;
            foreach (var row in table.Elements<TableRow>())
            {
                var tag = isFirstRow ? "th" : "td";
                sb.AppendLine("<tr>");
                foreach (var cell in row.Elements<TableCell>())
                {
                    var cellHtml = new StringBuilder();
                    foreach (var p in cell.Elements<Paragraph>())
                    {
                        cellHtml.Append(FormatRunsToHtml(p, imageMap));
                    }
                    sb.AppendLine($"<{tag} class=\"lecture-table-cell\">{cellHtml}</{tag}>");
                }
                sb.AppendLine("</tr>");
                if (isFirstRow)
                {
                    isFirstRow = false;
                }
            }

            sb.AppendLine("</table></div>");
            return sb.ToString();
        }

        private bool TryDetectQuiz(string text, ref string? currentQuestion, ref string? optA, ref string? optB, ref string? optC, ref string? optD, ref int currentQuizSectionId, List<LectureQuiz> quizzes, int currentDocSectionIndex)
        {
            // Detect question: starts with "Câu", "Cau", "Question" followed by number, OR just a number "1. "
            var questionMatch = Regex.Match(text, @"^(?:(?:C[aâ]u|Question)\s*)?\d+[\.\:\)\s]+(.*)", RegexOptions.IgnoreCase);
            if (questionMatch.Success)
            {
                FlushQuiz(ref currentQuestion, ref optA, ref optB, ref optC, ref optD, quizzes, currentQuizSectionId);
                currentQuestion = text;
                currentQuizSectionId = currentDocSectionIndex;
                return true;
            }

            if (currentQuestion != null)
            {
                // Check if all A,B,C,D are on the same line
                var sameLineOptions = Regex.Match(text, @"^\s*\(?A[\.\:\)]\s*(.+?)\s+\(?B[\.\:\)]\s*(.+?)\s+\(?C[\.\:\)]\s*(.+?)\s+\(?D[\.\:\)]\s*(.+)$", RegexOptions.IgnoreCase);
                if (sameLineOptions.Success)
                {
                    optA = sameLineOptions.Groups[1].Value.Trim();
                    optB = sameLineOptions.Groups[2].Value.Trim();
                    optC = sameLineOptions.Groups[3].Value.Trim();
                    optD = sameLineOptions.Groups[4].Value.Trim();
                    return true;
                }

                // Check 2 options on same line
                var twoOptionsAB = Regex.Match(text, @"^\s*\(?A[\.\:\)]\s*(.+?)\s+\(?B[\.\:\)]\s*(.+)$", RegexOptions.IgnoreCase);
                if (twoOptionsAB.Success)
                {
                    optA = twoOptionsAB.Groups[1].Value.Trim();
                    optB = twoOptionsAB.Groups[2].Value.Trim();
                    return true;
                }
                
                var twoOptionsCD = Regex.Match(text, @"^\s*\(?C[\.\:\)]\s*(.+?)\s+\(?D[\.\:\)]\s*(.+)$", RegexOptions.IgnoreCase);
                if (twoOptionsCD.Success)
                {
                    optC = twoOptionsCD.Groups[1].Value.Trim();
                    optD = twoOptionsCD.Groups[2].Value.Trim();
                    return true;
                }

                // Single option per line
                var optionMatch = Regex.Match(text, @"^\s*\(?([A-D])[\.\:\)]\s*(.*)", RegexOptions.IgnoreCase);
                if (optionMatch.Success)
                {
                    var letter = optionMatch.Groups[1].Value.ToUpper();
                    var optionText = optionMatch.Groups[2].Value.Trim();
                    switch (letter)
                    {
                        case "A": optA = optionText; break;
                        case "B": optB = optionText; break;
                        case "C": optC = optionText; break;
                        case "D": optD = optionText; break;
                    }
                    return true;
                }

                // If we get here, it means the line is NOT an option.
                // This implies the currentQuestion was just a numbered heading or normal text.
                // We should clear the state unless we already have some options (maybe we're at the end of a quiz).
                // Wait, if we already have A and B, we shouldn't clear, we should Flush!
                if (optA != null && optB != null)
                {
                    FlushQuiz(ref currentQuestion, ref optA, ref optB, ref optC, ref optD, quizzes, currentQuizSectionId);
                }
                else
                {
                    // It was a false alarm (e.g., a heading)
                    currentQuestion = null;
                    optA = optB = optC = optD = null;
                }
            }

            return false;
        }

        private void FlushQuiz(ref string? currentQuestion, ref string? optA, ref string? optB, ref string? optC, ref string? optD, List<LectureQuiz> quizzes, int sectionId)
        {
            if (currentQuestion != null && optA != null && optB != null)
            {
                quizzes.Add(new LectureQuiz
                {
                    QuestionText = currentQuestion,
                    OptionA = optA,
                    OptionB = optB,
                    OptionC = optC,
                    OptionD = optD,
                    CorrectAnswer = "A",
                    SortOrder = quizzes.Count + 1,
                    SectionId = sectionId > 0 ? sectionId : null
                });
            }
            currentQuestion = null;
            optA = optB = optC = optD = null;
        }
    }
}
