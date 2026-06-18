using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LexiLearn.Data;

namespace LexiLearn.ViewComponents
{
    public class PinnedTabsViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public PinnedTabsViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var claimsPrincipal = UserClaimsPrincipal;
            if (claimsPrincipal == null) return Content("");

            var userIdStr = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Content(""); // Return empty if not logged in
            }

            var pinnedItems = await _context.PinnedItems
                .Where(p => p.UserId == userId)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();

            return View(pinnedItems);
        }
    }
}
