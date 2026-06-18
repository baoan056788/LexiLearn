using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using LexiLearn.Services;

namespace LexiLearn.Controllers
{
    [Authorize]
    public class ProgressController : Controller
    {
        private readonly ProgressService _progressService;

        public ProgressController(ProgressService progressService)
        {
            _progressService = progressService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        public async Task<IActionResult> Dashboard()
        {
            var model = await _progressService.GetDashboardDataAsync(GetUserId());
            return View(model);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var progress = await _progressService.GetProgressAsync(GetUserId(), id);
            return View(progress);
        }

        public async Task<IActionResult> AllProgress()
        {
            var progresses = await _progressService.GetAllProgressAsync(GetUserId());
            return View(progresses);
        }
    }
}
