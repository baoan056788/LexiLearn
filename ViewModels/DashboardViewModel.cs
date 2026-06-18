using LexiLearn.Models;

namespace LexiLearn.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalSets { get; set; }
        public int TotalCards { get; set; }
        public int LearnedCards { get; set; }
        public int Streak { get; set; }
        public double OverallMastery { get; set; }
        public int TotalTests { get; set; }
        public double AverageScore { get; set; }
        public List<VocabularySet> RecentSets { get; set; } = new List<VocabularySet>();
        public List<Test> RecentTests { get; set; } = new List<Test>();
        public List<StudySession> RecentSessions { get; set; } = new List<StudySession>();
    }

    public class ExploreViewModel
    {
        public string? SearchQuery { get; set; }
        public int? CategoryId { get; set; }
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<VocabularySet> Sets { get; set; } = new List<VocabularySet>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalSets { get; set; }
        public int TotalCards { get; set; }
        public int TotalStudySessions { get; set; }
        public int PendingReports { get; set; }
        public List<User> RecentUsers { get; set; } = new List<User>();
    }
}
