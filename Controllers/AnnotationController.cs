using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LexiLearn.Data;
using LexiLearn.Models;

namespace LexiLearn.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AnnotationController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AnnotationController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        public class AnnotationRequest
        {
            public int LectureId { get; set; }
            public int? SectionId { get; set; }
            public int StartOffset { get; set; }
            public int EndOffset { get; set; }
            public string? XPath { get; set; }
            public string? SelectedText { get; set; }
            public string Type { get; set; } = "Highlight";
            public string? Color { get; set; }
            public string? Note { get; set; }
        }

        [HttpPost("Save")]
        public async Task<IActionResult> Save([FromBody] AnnotationRequest request)
        {
            var userId = GetUserId();

            var annotation = new UserAnnotation
            {
                UserId = userId,
                LectureId = request.LectureId,
                SectionId = request.SectionId,
                StartOffset = request.StartOffset,
                EndOffset = request.EndOffset,
                XPath = request.XPath,
                SelectedText = request.SelectedText,
                Type = request.Type,
                Color = request.Color,
                Note = request.Note,
                CreatedAt = DateTime.Now
            };

            _context.UserAnnotations.Add(annotation);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, annotationId = annotation.AnnotationId });
        }

        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            var annotation = await _context.UserAnnotations
                .FirstOrDefaultAsync(a => a.AnnotationId == id && a.UserId == userId);

            if (annotation == null) return NotFound();

            _context.UserAnnotations.Remove(annotation);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }
}
