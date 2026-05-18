using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_doc_truyen_Co_ban.Data;

namespace web_doc_truyen_Co_ban.Controllers
{
    [Authorize]
    public class ReadingHistoryController : Controller
    {
        
        private readonly ApplicationDbContext _dbcontext;

        public ReadingHistoryController(ApplicationDbContext db)
        {
            _dbcontext = db;
        }

        public async Task<IActionResult> Index()
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login", "Account");

            var userId = Guid.Parse(userIdStr);

            var histories = await _dbcontext.ReadingHistories
                .Include(h => h.Story)
                    .ThenInclude(s => s.Category)
                .Include(h => h.Chapter)
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.LastReadAt)
                .ToListAsync();

            return View(histories);
        }

        // Xóa 1 lịch sử
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login", "Account");

            var userId = Guid.Parse(userIdStr);

            var history = await _dbcontext.ReadingHistories
                .FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId);

            if (history != null)
            {
                _dbcontext.ReadingHistories.Remove(history);
                await _dbcontext.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        // Xóa toàn bộ lịch sử
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAll()
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login", "Account");

            var userId = Guid.Parse(userIdStr);

            var histories = await _dbcontext.ReadingHistories
                .Where(h => h.UserId == userId)
                .ToListAsync();

            _dbcontext.ReadingHistories.RemoveRange(histories);
            await _dbcontext.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}
