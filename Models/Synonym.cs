using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LexiLearn.Models
{
    public class Synonym
    {
        [Key]
        public int Id { get; set; }

        public int WordId { get; set; }

        [Required, MaxLength(100)]
        public string SynonymWord { get; set; } = string.Empty;

        [ForeignKey("WordId")]
        public WordDefinition? WordDefinition { get; set; }
    }
}
