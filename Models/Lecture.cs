using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LexiLearn.Models
{
    public class Lecture
    {
        [Key]
        public int LectureId { get; set; }

        public int UserId { get; set; }

        public int? CategoryId { get; set; }

        public int? CourseId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(255)]
        public string? OriginalFileName { get; set; }

        [MaxLength(500)]
        public string? FilePath { get; set; }

        public string? HtmlContent { get; set; }

        public bool IsPublic { get; set; } = false;

        public int SortOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        [ForeignKey("CourseId")]
        public LectureCourse? Course { get; set; }

        public ICollection<LectureSection> Sections { get; set; } = new List<LectureSection>();
        public ICollection<UserAnnotation> Annotations { get; set; } = new List<UserAnnotation>();
        public ICollection<LectureQuiz> Quizzes { get; set; } = new List<LectureQuiz>();
    }
}
