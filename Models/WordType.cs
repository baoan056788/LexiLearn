using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LexiLearn.Models
{
    public class WordType
    {
        [Key]
        public int Id { get; set; }

        public int WordId { get; set; }

        [Required, MaxLength(50)]
        public string PartOfSpeech { get; set; } = string.Empty; // noun, verb, adjective, adverb

        [Required]
        public string Meaning { get; set; } = string.Empty;

        public string? Example { get; set; }

        [ForeignKey("WordId")]
        public WordDefinition? WordDefinition { get; set; }
    }
}
