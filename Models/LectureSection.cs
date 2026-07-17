using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LexiLearn.Models
{
    public class LectureSection
    {
        [Key]
        public int SectionId { get; set; }

        public int LectureId { get; set; }

        public int? ParentSectionId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? HtmlContent { get; set; }

        public int SortOrder { get; set; } = 0;

        [MaxLength(100)]
        public string? AnchorId { get; set; }

        public int HeadingLevel { get; set; } = 1;

        [ForeignKey("LectureId")]
        public Lecture? Lecture { get; set; }

        [ForeignKey("ParentSectionId")]
        public LectureSection? ParentSection { get; set; }

        public ICollection<LectureSection> ChildSections { get; set; } = new List<LectureSection>();
        public ICollection<LectureQuiz> Quizzes { get; set; } = new List<LectureQuiz>();
    }
}
