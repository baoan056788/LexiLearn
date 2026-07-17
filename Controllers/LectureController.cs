using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LexiLearn.Data;
using LexiLearn.Models;
using LexiLearn.ViewModels;
using LexiLearn.Services;
using System.IO;

namespace LexiLearn.Controllers
{
    [Authorize]
    public class LectureController : Controller
    {
        private readonly AppDbContext _context;
        private readonly WordParserService _wordParser;

        public LectureController(AppDbContext context, WordParserService wordParser)
        {
            _context = context;
            _wordParser = wordParser;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        // GET: /Lecture
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            var lectures = await _context.Lectures
                .AsNoTracking()
                .Include(l => l.Category)
                .Include(l => l.Course)
                .Include(l => l.Sections)
                .Include(l => l.Quizzes)
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return View(lectures);
        }

        // GET: /Lecture/Explore
        [AllowAnonymous]
        public async Task<IActionResult> Explore(string? search, int? categoryId)
        {
            var query = _context.Lectures
                .AsNoTracking()
                .Include(l => l.Category)
                .Include(l => l.User)
                .Include(l => l.Sections)
                .Where(l => l.IsPublic);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(l => l.Title.Contains(search) || (l.Description != null && l.Description.Contains(search)));
            }
            if (categoryId.HasValue)
            {
                query = query.Where(l => l.CategoryId == categoryId);
            }

            var lectures = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();
            ViewBag.Categories = await _context.Categories.AsNoTracking().ToListAsync();
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            return View(lectures);
        }

        // GET: /Lecture/Upload
        [HttpGet]
        public async Task<IActionResult> Upload()
        {
            var userId = GetUserId();
            var model = new LectureUploadViewModel
            {
                Categories = await _context.Categories.AsNoTracking().ToListAsync(),
                Courses = await _context.LectureCourses.AsNoTracking().Where(c => c.UserId == userId).ToListAsync()
            };
            return View(model);
        }

        // POST: /Lecture/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(20 * 1024 * 1024)] // 20MB limit
        public async Task<IActionResult> Upload(LectureUploadViewModel model)
        {
            ModelState.Remove("WordFile");

            if (model.WordFile == null || model.WordFile.Length == 0)
            {
                ModelState.AddModelError("WordFile", "Vui long chon file Word (.docx)");
            }
            else if (!model.WordFile.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("WordFile", "Chi chap nhan file .docx");
            }

            if (!ModelState.IsValid)
            {
                var userId2 = GetUserId();
                model.Categories = await _context.Categories.AsNoTracking().ToListAsync();
                model.Courses = await _context.LectureCourses.AsNoTracking().Where(c => c.UserId == userId2).ToListAsync();
                return View(model);
            }

            var userId = GetUserId();

            var lecture = new Lecture
            {
                UserId = userId,
                CategoryId = model.CategoryId,
                CourseId = model.CourseId,
                Title = model.Title,
                Description = model.Description,
                OriginalFileName = model.WordFile!.FileName,
                IsPublic = model.IsPublic
            };

            _context.Lectures.Add(lecture);
            await _context.SaveChangesAsync();

            try
            {
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "lectures", lecture.LectureId.ToString());
                if (!Directory.Exists(uploadsDir))
                    Directory.CreateDirectory(uploadsDir);

                var originalFilePath = Path.Combine(uploadsDir, model.WordFile.FileName);
                using (var fileStream = new FileStream(originalFilePath, FileMode.Create))
                {
                    await model.WordFile.CopyToAsync(fileStream);
                }
                lecture.FilePath = $"/uploads/lectures/{lecture.LectureId}/{model.WordFile.FileName}";

                var parseResult = await _wordParser.ParseAsync(model.WordFile, lecture.LectureId);

                lecture.HtmlContent = parseResult.HtmlContent;
                lecture.UpdatedAt = DateTime.Now;

                foreach (var sectionInfo in parseResult.Sections)
                {
                    var section = new LectureSection
                    {
                        LectureId = lecture.LectureId,
                        Title = sectionInfo.Title,
                        AnchorId = sectionInfo.AnchorId,
                        HeadingLevel = sectionInfo.HeadingLevel,
                        SortOrder = sectionInfo.SortOrder
                    };
                    _context.LectureSections.Add(section);
                }

                foreach (var quiz in parseResult.Quizzes)
                {
                    quiz.LectureId = lecture.LectureId;
                    _context.LectureQuizzes.Add(quiz);
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Upload bai giang thanh cong! Tim thay {parseResult.Sections.Count} muc va {parseResult.Quizzes.Count} cau hoi trac nghiem.";
                return RedirectToAction("Details", new { id = lecture.LectureId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Loi khi xu ly file Word: {ex.Message}";
                return RedirectToAction("Details", new { id = lecture.LectureId });
            }
        }

        // GET: /Lecture/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = string.IsNullOrEmpty(userIdStr) ? 0 : int.Parse(userIdStr);
            
            var lecture = await _context.Lectures
                .AsNoTracking()
                .Include(l => l.Category)
                .Include(l => l.User)
                .Include(l => l.Course)
                .Include(l => l.Sections.OrderBy(s => s.SortOrder))
                .Include(l => l.Quizzes.OrderBy(q => q.SortOrder))
                .FirstOrDefaultAsync(l => l.LectureId == id);

            if (lecture == null) return NotFound();

            if (lecture.UserId != userId && !lecture.IsPublic)
                return Forbid();

            var annotations = new List<UserAnnotation>();
            if (userId > 0)
            {
                annotations = await _context.UserAnnotations
                    .AsNoTracking()
                    .Where(a => a.LectureId == id && a.UserId == userId)
                    .ToListAsync();
            }

            var viewModel = new LectureReaderViewModel
            {
                Lecture = lecture,
                Sections = lecture.Sections.ToList(),
                UserAnnotations = annotations,
                Quizzes = lecture.Quizzes.ToList(),
                IsOwner = lecture.UserId == userId
            };

            return View(viewModel);
        }

        // GET: /Lecture/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = GetUserId();
            var lecture = await _context.Lectures
                .FirstOrDefaultAsync(l => l.LectureId == id && l.UserId == userId);

            if (lecture == null) return NotFound();

            var model = new LectureUploadViewModel
            {
                LectureId = lecture.LectureId,
                Title = lecture.Title,
                Description = lecture.Description,
                CategoryId = lecture.CategoryId,
                CourseId = lecture.CourseId,
                IsPublic = lecture.IsPublic,
                Categories = await _context.Categories.AsNoTracking().ToListAsync(),
                Courses = await _context.LectureCourses.AsNoTracking().Where(c => c.UserId == userId).ToListAsync()
            };

            return View(model);
        }

        // POST: /Lecture/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(LectureUploadViewModel model)
        {
            ModelState.Remove("WordFile");

            if (!ModelState.IsValid)
            {
                var uid = GetUserId();
                model.Categories = await _context.Categories.AsNoTracking().ToListAsync();
                model.Courses = await _context.LectureCourses.AsNoTracking().Where(c => c.UserId == uid).ToListAsync();
                return View(model);
            }

            var userId = GetUserId();
            var lecture = await _context.Lectures
                .FirstOrDefaultAsync(l => l.LectureId == model.LectureId && l.UserId == userId);

            if (lecture == null) return NotFound();

            lecture.Title = model.Title;
            lecture.Description = model.Description;
            lecture.CategoryId = model.CategoryId;
            lecture.CourseId = model.CourseId;
            lecture.IsPublic = model.IsPublic;
            lecture.UpdatedAt = DateTime.Now;

            if (model.WordFile != null && model.WordFile.Length > 0)
            {
                if (!model.WordFile.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("WordFile", "Chi chap nhan file .docx");
                    var uid = GetUserId();
                    model.Categories = await _context.Categories.AsNoTracking().ToListAsync();
                    model.Courses = await _context.LectureCourses.AsNoTracking().Where(c => c.UserId == uid).ToListAsync();
                    return View(model);
                }

                var oldSections = await _context.LectureSections.Where(s => s.LectureId == lecture.LectureId).ToListAsync();
                var oldQuizzes = await _context.LectureQuizzes.Where(q => q.LectureId == lecture.LectureId).ToListAsync();
                _context.LectureSections.RemoveRange(oldSections);
                _context.LectureQuizzes.RemoveRange(oldQuizzes);

                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "lectures", lecture.LectureId.ToString());
                if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);
                var originalFilePath = Path.Combine(uploadsDir, model.WordFile.FileName);
                using (var fs = new FileStream(originalFilePath, FileMode.Create))
                {
                    await model.WordFile.CopyToAsync(fs);
                }
                lecture.FilePath = $"/uploads/lectures/{lecture.LectureId}/{model.WordFile.FileName}";
                lecture.OriginalFileName = model.WordFile.FileName;

                var parseResult = await _wordParser.ParseAsync(model.WordFile, lecture.LectureId);
                lecture.HtmlContent = parseResult.HtmlContent;

                foreach (var si in parseResult.Sections)
                {
                    _context.LectureSections.Add(new LectureSection
                    {
                        LectureId = lecture.LectureId,
                        Title = si.Title,
                        AnchorId = si.AnchorId,
                        HeadingLevel = si.HeadingLevel,
                        SortOrder = si.SortOrder
                    });
                }
                foreach (var q in parseResult.Quizzes)
                {
                    q.LectureId = lecture.LectureId;
                    _context.LectureQuizzes.Add(q);
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Cap nhat bai giang thanh cong!";
            return RedirectToAction("Details", new { id = lecture.LectureId });
        }

        [HttpGet]
        public async Task<IActionResult> GetLectureNote(int lectureId)
        {
            var userId = GetUserId();
            var note = await _context.StudyNotes
                .FirstOrDefaultAsync(n => n.LectureId == lectureId && n.UserId == userId);
            
            return Json(new { success = true, content = note?.Content ?? "" });
        }

        [HttpPost]
        public async Task<IActionResult> SaveLectureNote(int lectureId, [FromBody] string content)
        {
            var userId = GetUserId();
            var lecture = await _context.Lectures.FindAsync(lectureId);
            if (lecture == null) return NotFound();

            var note = await _context.StudyNotes
                .FirstOrDefaultAsync(n => n.LectureId == lectureId && n.UserId == userId);

            if (note == null)
            {
                note = new StudyNote
                {
                    UserId = userId,
                    LectureId = lectureId,
                    Title = "Ghi chú bài giảng: " + lecture.Title,
                    Content = content ?? ""
                };
                _context.StudyNotes.Add(note);
            }
            else
            {
                note.Content = content ?? "";
                note.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // POST: /Lecture/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            var lecture = await _context.Lectures
                .FirstOrDefaultAsync(l => l.LectureId == id && l.UserId == userId);

            if (lecture == null) return NotFound();

            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "lectures", lecture.LectureId.ToString());
            if (Directory.Exists(uploadsDir))
            {
                try { Directory.Delete(uploadsDir, true); } catch { }
            }

            _context.Lectures.Remove(lecture);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xoa bai giang thanh cong!";
            return RedirectToAction("Index");
        }

        // GET: /Lecture/Download/5
        [AllowAnonymous]
        public async Task<IActionResult> Download(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = string.IsNullOrEmpty(userIdStr) ? 0 : int.Parse(userIdStr);
            var lecture = await _context.Lectures
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.LectureId == id && (l.UserId == userId || l.IsPublic));

            if (lecture == null || string.IsNullOrEmpty(lecture.FilePath)) return NotFound();

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", lecture.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath)) return NotFound();

            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", lecture.OriginalFileName ?? "document.docx");
        }

        // === Quiz Actions ===

        // GET: /Lecture/Quiz/5
        [AllowAnonymous]
        public async Task<IActionResult> Quiz(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = string.IsNullOrEmpty(userIdStr) ? 0 : int.Parse(userIdStr);
            var lecture = await _context.Lectures
                .AsNoTracking()
                .Include(l => l.Quizzes.OrderBy(q => q.SortOrder))
                .FirstOrDefaultAsync(l => l.LectureId == id && (l.UserId == userId || l.IsPublic));

            if (lecture == null) return NotFound();

            if (!lecture.Quizzes.Any())
            {
                TempData["Error"] = "Bai giang nay khong co cau hoi trac nghiem.";
                return RedirectToAction("Details", new { id });
            }

            var vm = new LectureQuizViewModel
            {
                LectureId = lecture.LectureId,
                LectureTitle = lecture.Title,
                Questions = lecture.Quizzes.Select(q => new QuizItemViewModel
                {
                    QuizId = q.QuizId,
                    QuestionText = q.QuestionText,
                    OptionA = q.OptionA,
                    OptionB = q.OptionB,
                    OptionC = q.OptionC,
                    OptionD = q.OptionD,
                    CorrectAnswer = q.CorrectAnswer,
                    Explanation = q.Explanation
                }).ToList()
            };

            return View(vm);
        }

        // POST: /Lecture/SubmitQuiz
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> SubmitQuiz(int lectureId, Dictionary<int, string> answers)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = string.IsNullOrEmpty(userIdStr) ? 0 : int.Parse(userIdStr);
            var lecture = await _context.Lectures
                .AsNoTracking()
                .Include(l => l.Quizzes)
                .FirstOrDefaultAsync(l => l.LectureId == lectureId && (l.UserId == userId || l.IsPublic));

            if (lecture == null) return NotFound();

            var questions = lecture.Quizzes.Select(q => new QuizItemViewModel
            {
                QuizId = q.QuizId,
                QuestionText = q.QuestionText,
                OptionA = q.OptionA,
                OptionB = q.OptionB,
                OptionC = q.OptionC,
                OptionD = q.OptionD,
                CorrectAnswer = q.CorrectAnswer,
                Explanation = q.Explanation,
                UserAnswer = answers.ContainsKey(q.QuizId) ? answers[q.QuizId] : null
            }).ToList();

            var result = new QuizResultViewModel
            {
                LectureId = lecture.LectureId,
                LectureTitle = lecture.Title,
                TotalQuestions = questions.Count,
                CorrectCount = questions.Count(q => q.UserAnswer == q.CorrectAnswer),
                Questions = questions
            };

            return View("QuizResult", result);
        }

        // POST: /Lecture/CheckAnswer
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CheckAnswer([FromBody] LexiLearn.ViewModels.CheckQuizRequest request, [FromServices] LexiLearn.Services.GeminiQuizService _geminiQuizService)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = string.IsNullOrEmpty(userIdStr) ? 0 : int.Parse(userIdStr);
            var quiz = await _context.LectureQuizzes
                .Include(q => q.Lecture)
                .FirstOrDefaultAsync(q => q.QuizId == request.QuizId);

            if (quiz == null) return NotFound(new { error = "Quiz not found" });

            if (quiz.Lecture.UserId != userId && !quiz.Lecture.IsPublic)
                return Forbid();

            // If Explanation is missing or it's a generic "A" answer from Word parser without explanation
            if (string.IsNullOrEmpty(quiz.Explanation))
            {
                try
                {
                    var aiResult = await _geminiQuizService.EvaluateQuizAsync(quiz);
                    quiz.CorrectAnswer = aiResult.CorrectAnswer;
                    quiz.Explanation = aiResult.ExplanationHtml;
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { error = "AI Evaluation failed: " + ex.Message });
                }
            }

            bool isCorrect = string.Equals(request.SelectedAnswer, quiz.CorrectAnswer, StringComparison.OrdinalIgnoreCase);

            return Json(new
            {
                isCorrect = isCorrect,
                correctAnswer = quiz.CorrectAnswer,
                explanationHtml = quiz.Explanation
            });
        }
    }
}
