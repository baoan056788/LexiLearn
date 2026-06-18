using LexiLearn.Models;
using Microsoft.EntityFrameworkCore;

namespace LexiLearn.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using var context = serviceProvider.GetRequiredService<AppDbContext>();

            context.Database.EnsureCreated();

            // Seed Roles
            if (!context.Roles.Any())
            {
                context.Roles.AddRange(
                    new Role { RoleName = "Admin" },
                    new Role { RoleName = "Learner" }
                );
                context.SaveChanges();
            }

            // Seed Admin user
            if (!context.Users.Any(u => u.Email == "admin@lexilearn.com"))
            {
                var adminRole = context.Roles.First(r => r.RoleName == "Admin");
                context.Users.Add(new User
                {
                    RoleId = adminRole.RoleId,
                    FullName = "Administrator",
                    Email = "admin@lexilearn.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    IsActive = true
                });
                context.SaveChanges();
            }

            // Seed Categories
            if (!context.Categories.Any())
            {
                context.Categories.AddRange(
                    new Category { CategoryName = "TOEIC", Description = "Từ vựng luyện thi TOEIC" },
                    new Category { CategoryName = "IELTS", Description = "Từ vựng luyện thi IELTS" },
                    new Category { CategoryName = "IT Vocabulary", Description = "Từ vựng chuyên ngành Công nghệ thông tin" },
                    new Category { CategoryName = "Daily English", Description = "Từ vựng giao tiếp hàng ngày" },
                    new Category { CategoryName = "Business English", Description = "Từ vựng tiếng Anh thương mại" },
                    new Category { CategoryName = "Academic", Description = "Từ vựng học thuật" }
                );
                context.SaveChanges();
            }

            // Seed sample vocabulary set
            if (!context.VocabularySets.Any())
            {
                var admin = context.Users.First(u => u.Email == "admin@lexilearn.com");
                var toeicCat = context.Categories.First(c => c.CategoryName == "TOEIC");

                var sampleSet = new VocabularySet
                {
                    UserId = admin.UserId,
                    CategoryId = toeicCat.CategoryId,
                    Title = "TOEIC 600 - Bộ từ cơ bản",
                    Description = "Bộ từ vựng TOEIC 600 điểm dành cho người mới bắt đầu",
                    IsPublic = true
                };
                context.VocabularySets.Add(sampleSet);
                context.SaveChanges();

                context.VocabularyCards.AddRange(
                    new VocabularyCard { SetId = sampleSet.SetId, Term = "Accomplish", Meaning = "Hoàn thành", Ipa = "/əˈkɑːm.plɪʃ/", Example = "She accomplished her goal of finishing the project on time." },
                    new VocabularyCard { SetId = sampleSet.SetId, Term = "Achieve", Meaning = "Đạt được", Ipa = "/əˈtʃiːv/", Example = "He achieved great success in his career." },
                    new VocabularyCard { SetId = sampleSet.SetId, Term = "Approximately", Meaning = "Xấp xỉ, khoảng", Ipa = "/əˈprɑːk.sə.mət.li/", Example = "The project will take approximately three months." },
                    new VocabularyCard { SetId = sampleSet.SetId, Term = "Budget", Meaning = "Ngân sách", Ipa = "/ˈbʌdʒ.ɪt/", Example = "We need to stay within our budget." },
                    new VocabularyCard { SetId = sampleSet.SetId, Term = "Candidate", Meaning = "Ứng viên", Ipa = "/ˈkæn.dɪ.deɪt/", Example = "There are five candidates for the position." },
                    new VocabularyCard { SetId = sampleSet.SetId, Term = "Convenient", Meaning = "Tiện lợi", Ipa = "/kənˈviː.ni.ənt/", Example = "The hotel is convenient for the airport." },
                    new VocabularyCard { SetId = sampleSet.SetId, Term = "Deadline", Meaning = "Hạn chót", Ipa = "/ˈded.laɪn/", Example = "The deadline for applications is next Friday." },
                    new VocabularyCard { SetId = sampleSet.SetId, Term = "Efficient", Meaning = "Hiệu quả", Ipa = "/ɪˈfɪʃ.ənt/", Example = "The new system is more efficient than the old one." },
                    new VocabularyCard { SetId = sampleSet.SetId, Term = "Implement", Meaning = "Thực hiện, triển khai", Ipa = "/ˈɪm.plɪ.ment/", Example = "We plan to implement the new policy next month." },
                    new VocabularyCard { SetId = sampleSet.SetId, Term = "Negotiate", Meaning = "Đàm phán", Ipa = "/nɪˈɡoʊ.ʃi.eɪt/", Example = "They negotiated a new contract with the supplier." }
                );
                context.SaveChanges();
            }
        }
    }
}
