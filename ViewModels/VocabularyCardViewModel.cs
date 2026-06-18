using System.ComponentModel.DataAnnotations;

namespace LexiLearn.ViewModels
{
    public class VocabularyCardViewModel
    {
        public int CardId { get; set; }

        public int SetId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập từ tiếng Anh")]
        [MaxLength(100)]
        [Display(Name = "Từ tiếng Anh")]
        public string Term { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập nghĩa")]
        [MaxLength(255)]
        [Display(Name = "Nghĩa tiếng Việt")]
        public string Meaning { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "Phiên âm IPA")]
        public string? Ipa { get; set; }

        [MaxLength(500)]
        [Display(Name = "Ví dụ")]
        public string? Example { get; set; }

        [MaxLength(255)]
        [Display(Name = "URL hình ảnh")]
        public string? ImageUrl { get; set; }
    }
}
