using System.ComponentModel.DataAnnotations;

namespace LexiLearn.Models
{
    public class SystemSetting
    {
        [Key]
        [MaxLength(100)]
        public string SettingKey { get; set; } = string.Empty;

        [Required]
        public string SettingValue { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Description { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
