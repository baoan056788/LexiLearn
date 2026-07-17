using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LexiLearn.Models
{
    public class LectureCourse
    {
        [Key]
        public int CourseId { get; set; }

        public int UserId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? CoverImageUrl { get; set; }

        public bool IsPublic { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public User? User { get; set; }

        public ICollection<Lecture> Lectures { get; set; } = new List<Lecture>();
    }
}
