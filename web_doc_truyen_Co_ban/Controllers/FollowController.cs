using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Models;

namespace web_doc_truyen_Co_ban.Controllers
{
    public class FollowController : Controller
    {
        private readonly ILogger<FollowController> _logger;
        private readonly ApplicationDbContext _context;

        public FollowController(
            ILogger<FollowController> logger,
            ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // =========================
        // DANH SÁCH THEO DÕI
        // =========================
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var follows = await _context.Follows
                .Include(f => f.Story)
                .ThenInclude(s => s.Category)
                .Where(f => f.UserId.ToString() == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            // HOT
            ViewBag.HotStories = await _context.Stories
                .OrderByDescending(x => x.TotalViews + x.RatingSum)
                .Take(5)
                .ToListAsync();

            // NEW
            ViewBag.NewStories = await _context.Stories
                .OrderByDescending(x => x.UpdatedAt)
                .Take(5)
                .ToListAsync();

            return View(follows);
        }

        // =========================
        // THÊM FOLLOW
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFollow(Guid storyId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            // kiểm tra đã follow chưa
            var exists = await _context.Follows
                .AnyAsync(x =>
                    x.UserId.ToString() == userId &&
                    x.StoryId == storyId);

            if (!exists)
            {
                var follow = new FollowModel
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.Parse(userId),
                    StoryId = storyId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Follows.Add(follow);

                // tăng follow cho story
                var story = await _context.Stories
                    .FirstOrDefaultAsync(x => x.Id == storyId);

                if (story != null)
                {
                    story.TotalFollows += 1;
                    story.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Detail", "Story", new { id = storyId });
        }

        // =========================
        // BỎ FOLLOW
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFollow(Guid storyId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var follow = await _context.Follows
                .FirstOrDefaultAsync(x =>
                    x.UserId.ToString() == userId &&
                    x.StoryId == storyId);

            if (follow != null)
            {
                _context.Follows.Remove(follow);

                // giảm follow story
                var story = await _context.Stories
                    .FirstOrDefaultAsync(x => x.Id == storyId);

                if (story != null && story.TotalFollows > 0)
                {
                    story.TotalFollows -= 1;
                    story.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Detail", "Story", new { id = storyId });
        }
    }
}