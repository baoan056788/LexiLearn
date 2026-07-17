namespace LexiLearn.ViewModels
{
    public class StudyNoteSaveViewModel
    {
        public int? StudyNoteId { get; set; }
        public int? SetId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? RelatedTerm { get; set; }
        public string? Tags { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsPinned { get; set; }
    }

    public class StudyNoteSummaryViewModel
    {
        public int StudyNoteId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? RelatedTerm { get; set; }
        public string? Tags { get; set; }
        public string? SetTitle { get; set; }
        public bool IsPinned { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Preview { get; set; } = string.Empty;
    }

    public class StudyNoteDetailViewModel
    {
        public int StudyNoteId { get; set; }
        public int? SetId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? RelatedTerm { get; set; }
        public string? Tags { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsPinned { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
