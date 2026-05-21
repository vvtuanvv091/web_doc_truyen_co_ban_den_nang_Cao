using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Models;

namespace web_doc_truyen_Co_ban.Controllers
{
    [Authorize]
    public class ReadingHistoryController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;

        public ReadingHistoryController(ApplicationDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        // =========================
        // Trang lịch sử đọc
        // =========================
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();

            if (userId == Guid.Empty)
                return RedirectToAction("Login", "Account");

            var histories = await _dbcontext.ReadingHistories
                .AsNoTracking()
                .Include(h => h.Story)
                    .ThenInclude(s => s.Category)
                .Include(h => h.Chapter)
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.LastReadAt)
                .ToListAsync();

            return View(histories);
        }

        // =========================
        // Xóa 1 lịch sử
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetUserId();

            if (userId == Guid.Empty)
                return RedirectToAction("Login", "Account");

            var history = await _dbcontext.ReadingHistories
                .FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId);

            if (history != null)
            {
                _dbcontext.ReadingHistories.Remove(history);
                await _dbcontext.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // Xóa toàn bộ lịch sử
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAll()
        {
            var userId = GetUserId();

            if (userId == Guid.Empty)
                return RedirectToAction("Login", "Account");

            var histories = await _dbcontext.ReadingHistories
                .Where(h => h.UserId == userId)
                .ToListAsync();

            if (histories.Any())
            {
                _dbcontext.ReadingHistories.RemoveRange(histories);
                await _dbcontext.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // Helper lấy UserId
        // =========================
        private Guid GetUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(userIdStr, out Guid userId)
                ? userId
                : Guid.Empty;
        }
    }
}