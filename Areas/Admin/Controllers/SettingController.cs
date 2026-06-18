using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LexiLearn.Data;
using LexiLearn.Models;

namespace LexiLearn.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrator,Admin")]
    public class SettingController : Controller
    {
        private readonly AppDbContext _context;

        public SettingController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _context.SystemSettings.ToListAsync();
            
            // Seed default settings if empty
            if (!settings.Any())
            {
                var defaults = new List<SystemSetting>
                {
                    new SystemSetting { SettingKey = "SiteName", SettingValue = "LexiLearn", Description = "Tên website" },
                    new SystemSetting { SettingKey = "AllowRegistration", SettingValue = "true", Description = "Cho phép đăng ký thành viên mới" },
                    new SystemSetting { SettingKey = "MaxSetsPerUser", SettingValue = "50", Description = "Số lượng bộ từ tối đa mỗi người dùng" }
                };
                _context.SystemSettings.AddRange(defaults);
                await _context.SaveChangesAsync();
                settings = defaults;
            }

            return View(settings);
        }

        [HttpPost]
        public async Task<IActionResult> Update(Dictionary<string, string> settings)
        {
            var existingSettings = await _context.SystemSettings.ToListAsync();

            foreach (var kvp in settings)
            {
                var setting = existingSettings.FirstOrDefault(s => s.SettingKey == kvp.Key);
                if (setting != null)
                {
                    setting.SettingValue = kvp.Value;
                    setting.UpdatedAt = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã lưu cài đặt hệ thống.";

            return RedirectToAction(nameof(Index));
        }
    }
}
