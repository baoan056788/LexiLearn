using System.Security.Claims;
using LexiLearn.Data;
using LexiLearn.Models;
using LexiLearn.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LexiLearn.Controllers
{
    [Authorize]
    [Route("api/notebook")]
    public class NotebookController : Controller
    {
        private readonly AppDbContext _context;

        public NotebookController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        [HttpGet]
        public async Task<IActionResult> List(string? search, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var query = _context.StudyNotes
                .AsNoTracking()
                .Include(n => n.VocabularySet)
                .Where(n => n.UserId == userId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                query = query.Where(n =>
                    n.Title.Contains(keyword) ||
                    n.Content.Contains(keyword) ||
                    (n.Tags != null && n.Tags.Contains(keyword)) ||
                    (n.RelatedTerm != null && n.RelatedTerm.Contains(keyword)));
            }

            var notes = await query
                .OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.UpdatedAt)
                .Take(50)
                .Select(n => new StudyNoteSummaryViewModel
                {
                    StudyNoteId = n.StudyNoteId,
                    Title = n.Title,
                    RelatedTerm = n.RelatedTerm,
                    Tags = n.Tags,
                    SetTitle = n.VocabularySet != null ? n.VocabularySet.Title : null,
                    IsPinned = n.IsPinned,
                    UpdatedAt = n.UpdatedAt,
                    Preview = n.Content.Length > 140 ? n.Content.Substring(0, 140) + "..." : n.Content
                })
                .ToListAsync(cancellationToken);

            return Ok(notes);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var note = await _context.StudyNotes
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.StudyNoteId == id && n.UserId == userId, cancellationToken);

            if (note == null)
            {
                return NotFound(new { message = "Khong tim thay note." });
            }

            return Ok(new StudyNoteDetailViewModel
            {
                StudyNoteId = note.StudyNoteId,
                SetId = note.SetId,
                Title = note.Title,
                RelatedTerm = note.RelatedTerm,
                Tags = note.Tags,
                Content = note.Content,
                IsPinned = note.IsPinned,
                UpdatedAt = note.UpdatedAt
            });
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] StudyNoteSaveViewModel? request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Du lieu note khong hop le." });
            }

            var userId = GetUserId();
            var content = request.Content?.Trim() ?? string.Empty;
            var title = string.IsNullOrWhiteSpace(request.Title)
                ? BuildFallbackTitle(request.RelatedTerm, content)
                : request.Title.Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                return BadRequest(new { message = "Vui long nhap tieu de note." });
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return BadRequest(new { message = "Vui long nhap noi dung note." });
            }

            if (request.SetId.HasValue)
            {
                var ownsSet = await _context.VocabularySets
                    .AnyAsync(s => s.SetId == request.SetId.Value && s.UserId == userId, cancellationToken);
                if (!ownsSet)
                {
                    return BadRequest(new { message = "Bo tu duoc chon khong hop le." });
                }
            }

            StudyNote? note;
            if (request.StudyNoteId.HasValue)
            {
                note = await _context.StudyNotes
                    .FirstOrDefaultAsync(n => n.StudyNoteId == request.StudyNoteId.Value && n.UserId == userId, cancellationToken);

                if (note == null)
                {
                    return NotFound(new { message = "Khong tim thay note." });
                }
            }
            else
            {
                note = new StudyNote
                {
                    UserId = userId,
                    CreatedAt = DateTime.Now
                };
                _context.StudyNotes.Add(note);
            }

            note.SetId = request.SetId;
            note.Title = title;
            note.RelatedTerm = string.IsNullOrWhiteSpace(request.RelatedTerm) ? null : request.RelatedTerm.Trim();
            note.Tags = string.IsNullOrWhiteSpace(request.Tags) ? null : request.Tags.Trim();
            note.Content = content;
            note.IsPinned = request.IsPinned;
            note.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new { success = true, studyNoteId = note.StudyNoteId });
        }

        private static string BuildFallbackTitle(string? relatedTerm, string content)
        {
            if (!string.IsNullOrWhiteSpace(relatedTerm))
            {
                return $"Ghi chu {relatedTerm.Trim()}";
            }

            if (!string.IsNullOrWhiteSpace(content))
            {
                var firstLine = content
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(firstLine))
                {
                    return firstLine.Length > 80 ? firstLine[..80] : firstLine;
                }
            }

            return $"Ghi chu {DateTime.Now:dd/MM HH:mm}";
        }

        [HttpPost("{id:int}/pin")]
        public async Task<IActionResult> TogglePin(int id, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var note = await _context.StudyNotes
                .FirstOrDefaultAsync(n => n.StudyNoteId == id && n.UserId == userId, cancellationToken);

            if (note == null)
            {
                return NotFound(new { message = "Khong tim thay note." });
            }

            note.IsPinned = !note.IsPinned;
            note.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new { success = true, isPinned = note.IsPinned });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var note = await _context.StudyNotes
                .FirstOrDefaultAsync(n => n.StudyNoteId == id && n.UserId == userId, cancellationToken);

            if (note == null)
            {
                return NotFound(new { message = "Khong tim thay note." });
            }

            _context.StudyNotes.Remove(note);
            await _context.SaveChangesAsync(cancellationToken);
            return Ok(new { success = true });
        }
    }
}
