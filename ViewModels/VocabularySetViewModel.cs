using System.ComponentModel.DataAnnotations;
using LexiLearn.Models;

namespace LexiLearn.ViewModels
{
    public class VocabularySetViewModel
    {
        public int SetId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên bộ từ")]
        [MaxLength(150)]
        [Display(Name = "Tên bộ từ")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Danh mục")]
        public int? CategoryId { get; set; }

        [Display(Name = "Công khai")]
        public bool IsPublic { get; set; }

        [Display(Name = "Tải lên file Excel")]
        public IFormFile? ImportFile { get; set; }

        public List<Category> Categories { get; set; } = new List<Category>();
        public List<VocabularyCardViewModel> Cards { get; set; } = new List<VocabularyCardViewModel>();
    }
}
