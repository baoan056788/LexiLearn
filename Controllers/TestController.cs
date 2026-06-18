using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LexiLearn.Services;
using LexiLearn.ViewModels;
using LexiLearn.Data;

namespace LexiLearn.Controllers
{
    [Authorize]
    public class TestController : Controller
    {
        private readonly TestService _testService;
        private readonly AppDbContext _context;

        public TestController(TestService testService, AppDbContext context)
        {
            _testService = testService;
            _context = context;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        [HttpGet]
        public async Task<IActionResult> Setup(int id)
        {
            var set = await _context.VocabularySets.FirstOrDefaultAsync(s => s.SetId == id);
            if (set == null) return NotFound();

            var model = new TestSetupViewModel { SetId = id };
            ViewBag.SetTitle = set.Title;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Start(TestSetupViewModel setup)
        {
            var test = await _testService.GenerateTestAsync(setup);
            if (test == null)
            {
                TempData["Error"] = "Cần ít nhất 4 thẻ từ để tạo bài test!";
                return RedirectToAction("Setup", new { id = setup.SetId });
            }
            
            return View(test);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int setId, List<TestAnswerViewModel> answers)
        {
            if (answers == null || !answers.Any())
            {
                TempData["Error"] = "Vui lòng trả lời ít nhất một câu hỏi!";
                return RedirectToAction("Setup", new { id = setId });
            }

            var result = await _testService.SubmitTestAsync(GetUserId(), setId, answers);
            return RedirectToAction("Result", new { id = result.TestId });
        }

        public async Task<IActionResult> Result(int id)
        {
            var result = await _testService.GetTestResultAsync(id);
            if (result == null) return NotFound();

            return View(result);
        }

        public async Task<IActionResult> History()
        {
            var tests = await _testService.GetTestHistoryAsync(GetUserId());
            return View(tests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteHistory(int id)
        {
            var success = await _testService.DeleteTestHistoryAsync(id, GetUserId());
            if (success)
            {
                TempData["Success"] = "Đã xóa bài test khỏi lịch sử.";
            }
            else
            {
                TempData["Error"] = "Không thể xóa bài test này.";
            }
            return RedirectToAction("History");
        }
    }
}
