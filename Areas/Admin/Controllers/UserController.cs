using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LexiLearn.Data;
using LexiLearn.Models;

namespace LexiLearn.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrator,Admin")]
    public class UserController : Controller
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString, int? roleId, bool? isActive, int page = 1)
        {
            int pageSize = 10;
            var query = _context.Users.Include(u => u.Role).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(u => u.FullName.Contains(searchString) || u.Email.Contains(searchString));
            }

            if (roleId.HasValue)
            {
                query = query.Where(u => u.RoleId == roleId.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            var totalItems = await query.CountAsync();
            var users = await query.OrderByDescending(u => u.CreatedAt)
                                   .Skip((page - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync();

            ViewBag.Roles = await _context.Roles.ToListAsync();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.SearchString = searchString;
            ViewBag.RoleId = roleId;
            ViewBag.IsActive = isActive;

            return View(users);
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.VocabularySets)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Prevent locking oneself
            if (user.Email == User.Identity?.Name)
            {
                TempData["Error"] = "Không thể tự khóa tài khoản của chính mình.";
                return RedirectToAction(nameof(Index));
            }

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Tài khoản {user.FullName} đã được {(user.IsActive ? "mở khóa" : "khóa")}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateRole(int id, int newRoleId)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (user.Email == User.Identity?.Name)
            {
                TempData["Error"] = "Không thể thay đổi quyền của chính mình.";
                return RedirectToAction(nameof(Index));
            }

            var role = await _context.Roles.FindAsync(newRoleId);
            if (role == null) return NotFound();

            user.RoleId = newRoleId;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã cấp quyền {role.RoleName} cho {user.FullName}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
