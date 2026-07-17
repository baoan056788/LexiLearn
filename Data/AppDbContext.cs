using Microsoft.EntityFrameworkCore;
using LexiLearn.Models;

namespace LexiLearn.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<VocabularySet> VocabularySets { get; set; }
        public DbSet<VocabularyCard> VocabularyCards { get; set; }
        public DbSet<StudySession> StudySessions { get; set; }
        public DbSet<StudyResult> StudyResults { get; set; }
        public DbSet<Test> Tests { get; set; }
        public DbSet<TestQuestion> TestQuestions { get; set; }
        public DbSet<Progress> Progresses { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<PinnedItem> PinnedItems { get; set; }
        public DbSet<CardReview> CardReviews { get; set; }
        public DbSet<AiConversation> AiConversations { get; set; }
        public DbSet<AiMessage> AiMessages { get; set; }
        public DbSet<StudyNote> StudyNotes { get; set; }

        // New Admin Tables
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationRecipient> NotificationRecipients { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }

        // Internal Dictionary Tables
        public DbSet<WordDefinition> WordDefinitions { get; set; }
        public DbSet<WordType> WordTypes { get; set; }
        public DbSet<Synonym> Synonyms { get; set; }
        public DbSet<RelatedWord> RelatedWords { get; set; }

        // Lecture Module Tables
        public DbSet<Lecture> Lectures { get; set; }
        public DbSet<LectureSection> LectureSections { get; set; }
        public DbSet<LectureCourse> LectureCourses { get; set; }
        public DbSet<UserAnnotation> UserAnnotations { get; set; }
        public DbSet<LectureQuiz> LectureQuizzes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Role>()
                .HasIndex(r => r.RoleName)
                .IsUnique();

            modelBuilder.Entity<VocabularySet>()
                .HasIndex(vs => new { vs.IsPublic, vs.CreatedAt });

            modelBuilder.Entity<VocabularySet>()
                .HasIndex(vs => vs.UserId);

            modelBuilder.Entity<VocabularyCard>()
                .HasIndex(vc => vc.SetId);

            modelBuilder.Entity<Test>()
                .HasIndex(t => new { t.UserId, t.CreatedAt });

            modelBuilder.Entity<StudySession>()
                .HasIndex(ss => new { ss.UserId, ss.StartedAt });

            modelBuilder.Entity<Progress>()
                .HasIndex(p => new { p.UserId, p.SetId })
                .IsUnique();

            modelBuilder.Entity<CardReview>()
                .HasIndex(cr => new { cr.UserId, cr.CardId })
                .IsUnique();

            modelBuilder.Entity<CardReview>()
                .HasIndex(cr => new { cr.UserId, cr.DueAt });

            modelBuilder.Entity<AiConversation>()
                .HasIndex(c => new { c.UserId, c.UpdatedAt });

            modelBuilder.Entity<AiMessage>()
                .HasIndex(m => new { m.AiConversationId, m.CreatedAt });

            modelBuilder.Entity<StudyNote>()
                .HasIndex(n => new { n.UserId, n.UpdatedAt });

            modelBuilder.Entity<StudyNote>()
                .HasIndex(n => new { n.UserId, n.IsPinned, n.UpdatedAt });

            // Relationships
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VocabularySet>()
                .HasOne(vs => vs.User)
                .WithMany(u => u.VocabularySets)
                .HasForeignKey(vs => vs.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VocabularySet>()
                .HasOne(vs => vs.Category)
                .WithMany(c => c.VocabularySets)
                .HasForeignKey(vs => vs.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<VocabularyCard>()
                .HasOne(vc => vc.VocabularySet)
                .WithMany(vs => vs.Cards)
                .HasForeignKey(vc => vc.SetId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudySession>()
                .HasOne(ss => ss.User)
                .WithMany(u => u.StudySessions)
                .HasForeignKey(ss => ss.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudySession>()
                .HasOne(ss => ss.VocabularySet)
                .WithMany(vs => vs.StudySessions)
                .HasForeignKey(ss => ss.SetId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudyResult>()
                .HasOne(sr => sr.StudySession)
                .WithMany(ss => ss.Results)
                .HasForeignKey(sr => sr.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudyResult>()
                .HasOne(sr => sr.VocabularyCard)
                .WithMany(vc => vc.StudyResults)
                .HasForeignKey(sr => sr.CardId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Test>()
                .HasOne(t => t.User)
                .WithMany(u => u.Tests)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Test>()
                .HasOne(t => t.VocabularySet)
                .WithMany(vs => vs.Tests)
                .HasForeignKey(t => t.SetId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TestQuestion>()
                .HasOne(tq => tq.Test)
                .WithMany(t => t.Questions)
                .HasForeignKey(tq => tq.TestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TestQuestion>()
                .HasOne(tq => tq.VocabularyCard)
                .WithMany()
                .HasForeignKey(tq => tq.CardId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Progress>()
                .HasOne(p => p.User)
                .WithMany(u => u.Progresses)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Progress>()
                .HasOne(p => p.VocabularySet)
                .WithMany(vs => vs.Progresses)
                .HasForeignKey(p => p.SetId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.Reporter)
                .WithMany()
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.VocabularySet)
                .WithMany()
                .HasForeignKey(r => r.SetId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PinnedItem>()
                .HasOne(p => p.User)
                .WithMany(u => u.PinnedItems)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CardReview>()
                .HasOne(cr => cr.User)
                .WithMany(u => u.CardReviews)
                .HasForeignKey(cr => cr.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CardReview>()
                .HasOne(cr => cr.VocabularyCard)
                .WithMany(vc => vc.CardReviews)
                .HasForeignKey(cr => cr.CardId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<AiConversation>()
                .HasOne(c => c.User)
                .WithMany(u => u.AiConversations)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AiMessage>()
                .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.AiConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudyNote>()
                .HasOne(n => n.User)
                .WithMany(u => u.StudyNotes)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudyNote>()
                .HasOne(n => n.VocabularySet)
                .WithMany(vs => vs.StudyNotes)
                .HasForeignKey(n => n.SetId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.CreatedBy)
                .WithMany()
                .HasForeignKey(n => n.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<NotificationRecipient>()
                .HasOne(nr => nr.User)
                .WithMany()
                .HasForeignKey(nr => nr.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<NotificationRecipient>()
                .HasOne(nr => nr.Notification)
                .WithMany(n => n.Recipients)
                .HasForeignKey(nr => nr.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            // === Lecture Module ===
            modelBuilder.Entity<Lecture>()
                .HasIndex(l => new { l.UserId, l.CreatedAt });

            modelBuilder.Entity<Lecture>()
                .HasIndex(l => l.IsPublic);

            modelBuilder.Entity<UserAnnotation>()
                .HasIndex(a => new { a.UserId, a.LectureId });

            modelBuilder.Entity<LectureQuiz>()
                .HasIndex(q => q.LectureId);

            modelBuilder.Entity<Lecture>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Lecture>()
                .HasOne(l => l.Category)
                .WithMany()
                .HasForeignKey(l => l.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Lecture>()
                .HasOne(l => l.Course)
                .WithMany(c => c.Lectures)
                .HasForeignKey(l => l.CourseId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<LectureSection>()
                .HasOne(s => s.Lecture)
                .WithMany(l => l.Sections)
                .HasForeignKey(s => s.LectureId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LectureSection>()
                .HasOne(s => s.ParentSection)
                .WithMany(s => s.ChildSections)
                .HasForeignKey(s => s.ParentSectionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<LectureCourse>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserAnnotation>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserAnnotation>()
                .HasOne(a => a.Lecture)
                .WithMany(l => l.Annotations)
                .HasForeignKey(a => a.LectureId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<UserAnnotation>()
                .HasOne(a => a.Section)
                .WithMany()
                .HasForeignKey(a => a.SectionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<LectureQuiz>()
                .HasOne(q => q.Lecture)
                .WithMany(l => l.Quizzes)
                .HasForeignKey(q => q.LectureId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LectureQuiz>()
                .HasOne(q => q.Section)
                .WithMany(s => s.Quizzes)
                .HasForeignKey(q => q.SectionId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
