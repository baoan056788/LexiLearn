using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LexiLearn.Models
{
    public class UserAnnotation
    {
        [Key]
        public int AnnotationId { get; set; }

        public int UserId { get; set; }

        public int LectureId { get; set; }

        public int? SectionId { get; set; }

        public int StartOffset { get; set; }

        public int EndOffset { get; set; }

        [MaxLength(500)]
        public string? XPath { get; set; }

        public string? SelectedText { get; set; }

        [Required, MaxLength(30)]
        public string Type { get; set; } = "Highlight";

        [MaxLength(20)]
        public string? Color { get; set; } = "#FFF176";

        [MaxLength(1000)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("LectureId")]
        public Lecture? Lecture { get; set; }

        [ForeignKey("SectionId")]
        public LectureSection? Section { get; set; }
    }
}
