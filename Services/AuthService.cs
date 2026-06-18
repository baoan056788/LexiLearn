using LexiLearn.Data;
using LexiLearn.Models;
using Microsoft.EntityFrameworkCore;

namespace LexiLearn.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;

        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> LoginAsync(string email, string password)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

            if (user == null) return null;
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;

            return user;
        }

        public async Task<(bool Success, string Message)> RegisterAsync(string fullName, string email, string password)
        {
            if (await _context.Users.AnyAsync(u => u.Email == email))
                return (false, "Email đã được sử dụng");

            var learnerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Learner");
            if (learnerRole == null)
                return (false, "Lỗi hệ thống: không tìm thấy vai trò Learner");

            var user = new User
            {
                RoleId = learnerRole.RoleId,
                FullName = fullName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return (true, "Đăng ký thành công");
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<bool> UpdateProfileAsync(int userId, string fullName, string? avatarUrl)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.FullName = fullName;
            if (avatarUrl != null) user.AvatarUrl = avatarUrl;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
