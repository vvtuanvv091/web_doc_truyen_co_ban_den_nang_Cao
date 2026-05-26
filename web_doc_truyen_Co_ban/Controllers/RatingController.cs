using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Models;

namespace web_doc_truyen_Co_ban.Controllers
{
    public class RatingController : Controller
    {
        private readonly ILogger<RatingController> _logger;
        private readonly ApplicationDbContext _context;

        public RatingController(
            ILogger<RatingController> logger,
            ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // =========================
        // DANH SÁCH ĐÁNH GIÁ
        // =========================
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            // Đánh giá của user hiện tại
            var myRatings = await _context.Ratings
                .Include(r => r.Story)
                .ThenInclude(s => s.Category)
                .Where(r => r.UserId.ToString() == userId)
                .OrderByDescending(r => r.UpdatedAt)
                .ToListAsync();

            // Top truyện được đánh giá cao
            ViewBag.TopStories = await _context.Stories
                .Where(s => s.RatingCount > 0)
                .OrderByDescending(s => s.RatingAvg)
                .ThenByDescending(s => s.RatingCount)
                .Take(5)
                .ToListAsync();

            // Tất cả đánh giá gần đây (public)
            ViewBag.AllRatings = await _context.Ratings
                .Include(r => r.Story)
                .Include(r => r.User)
                .OrderByDescending(r => r.UpdatedAt)
                .Take(20)
                .ToListAsync();

            return View(myRatings);
        }

        // =========================
        // THÊM / CẬP NHẬT ĐÁNH GIÁ
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rate(Guid storyId, byte score)
        {
            if (score < 1 || score > 5)
                return Json(new { success = false, message = "Điểm không hợp lệ." });

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Bạn cần đăng nhập." });

            var story = await _context.Stories.FindAsync(storyId);
            if (story == null)
                return Json(new { success = false, message = "Không tìm thấy truyện." });

            var existing = await _context.Ratings
                .FirstOrDefaultAsync(r =>
                    r.UserId.ToString() == userId &&
                    r.StoryId == storyId);

            if (existing == null)
            {
                // Thêm mới
                _context.Ratings.Add(new RatingModel
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.Parse(userId),
                    StoryId = storyId,
                    Score = score,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                story.RatingSum += score;
                story.RatingCount += 1;
            }
            else
            {
                // Cập nhật
                story.RatingSum -= existing.Score;
                story.RatingSum += score;
                existing.Score = score;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            story.RatingAvg = story.RatingCount > 0
                ? Math.Round((decimal)story.RatingSum / story.RatingCount, 2) : 0;
            story.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                avgRating = story.RatingAvg,
                ratingCount = story.RatingCount,
                userScore = score
            });
        }

        // =========================
        // XÓA ĐÁNH GIÁ
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRating(Guid storyId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var rating = await _context.Ratings
                .FirstOrDefaultAsync(r =>
                    r.UserId.ToString() == userId &&
                    r.StoryId == storyId);

            if (rating != null)
            {
                _context.Ratings.Remove(rating);

                var story = await _context.Stories.FindAsync(storyId);
                if (story != null && story.RatingCount > 0)
                {
                    story.RatingSum -= rating.Score;
                    story.RatingCount -= 1;
                    story.RatingAvg = story.RatingCount > 0
                        ? Math.Round((decimal)story.RatingSum / story.RatingCount, 2) : 0;
                    story.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}