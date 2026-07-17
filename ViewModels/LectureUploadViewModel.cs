using System.ComponentModel.DataAnnotations;
using LexiLearn.Models;
using Microsoft.AspNetCore.Http;

namespace LexiLearn.ViewModels
{
    public class LectureUploadViewModel
    {
        public int LectureId { get; set; }

        [Required(ErrorMessage = "Vui long nhap tieu de bai giang")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public int? CategoryId { get; set; }

        public int? CourseId { get; set; }

        public bool IsPublic { get; set; } = false;

        [Required(ErrorMessage = "Vui long chon file Word (.docx)")]
        public IFormFile? WordFile { get; set; }

        // For dropdown lists
        public List<Category> Categories { get; set; } = new();
        public List<LectureCourse> Courses { get; set; } = new();
    }
}
