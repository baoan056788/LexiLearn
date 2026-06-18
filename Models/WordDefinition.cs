using System.ComponentModel.DataAnnotations;

namespace LexiLearn.Models
{
    public class WordDefinition
    {
        [Key]
        public int WordId { get; set; }

        [Required, MaxLength(100)]
        public string Word { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Phonetic { get; set; }

        [MaxLength(500)]
        public string? AudioUrl { get; set; }

        public ICollection<WordType> WordTypes { get; set; } = new List<WordType>();
        public ICollection<Synonym> Synonyms { get; set; } = new List<Synonym>();
        public ICollection<RelatedWord> RelatedWords { get; set; } = new List<RelatedWord>();
    }
}
