using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Models;

namespace web_doc_truyen_Co_ban.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class StoryRatingController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;

        public StoryRatingController(ApplicationDbContext context)
        {
            _dbcontext = context;
        }

        // =====================================================================
        // INDEX
        // =====================================================================
        public async Task<IActionResult> Index(string? search, string? storyId, int? score, int? status, int page = 1)
        {
            ViewData["ActiveMenu"] = "StoryRating";
            int pageSize = 15;

            var query = _dbcontext.Ratings
                .Include(r => r.User)
                .Include(r => r.Story)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(r => r.User.Username.ToLower().Contains(search));
                ViewData["Search"] = search;
            }

            if (!string.IsNullOrWhiteSpace(storyId) && Guid.TryParse(storyId, out var sid))
            {
                query = query.Where(r => r.StoryId == sid);
                ViewData["StoryId"] = storyId;
            }

            if (score.HasValue && score >= 1 && score <= 5)
            {
                query = query.Where(r => r.Score == (byte)score.Value);
                ViewData["Score"] = score;
            }

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == (byte)status.Value);
                ViewData["Status"] = status;
            }

            int total = await query.CountAsync();

            var ratings = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Thống kê
            ViewBag.TotalAll = await _dbcontext.Ratings.CountAsync();
            ViewBag.TotalVisible = await _dbcontext.Ratings.CountAsync(r => r.Status == 0);
            ViewBag.TotalHidden = await _dbcontext.Ratings.CountAsync(r => r.Status == 1);
            ViewBag.AvgScore = Math.Round(
                await _dbcontext.Ratings.AverageAsync(r => (double?)r.Score) ?? 0, 2);
            ViewBag.Total5Star = await _dbcontext.Ratings.CountAsync(r => r.Score == 5);
            ViewBag.Total1Star = await _dbcontext.Ratings.CountAsync(r => r.Score == 1);

            ViewBag.Stories = await _dbcontext.Stories
                .Select(s => new { s.Id, s.Title })
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

            return View(ratings);
        }

        // =====================================================================
        // DELETE (hard delete)
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            ViewData["ActiveMenu"] = "StoryRating";

            var rating = await _dbcontext.Ratings
                .Include(r => r.User)
                .Include(r => r.Story)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rating == null) return NotFound();
            return View(rating);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var rating = await _dbcontext.Ratings.FindAsync(id);
            if (rating == null) return NotFound();

            var storyId = rating.StoryId;

            _dbcontext.Ratings.Remove(rating);
            await _dbcontext.SaveChangesAsync();

            await RecalcStoryRating(storyId);

            TempData["Success"] = "Đã xóa đánh giá.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================================
        // DETAIL
        // =====================================================================
        public async Task<IActionResult> Detail(Guid id)
        {
            ViewData["ActiveMenu"] = "StoryRating";

            var rating = await _dbcontext.Ratings
                .Include(r => r.User)
                .Include(r => r.Story)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rating == null) return NotFound();
            return View(rating);
        }

        // =====================================================================
        // TOGGLE STATUS (AJAX)
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(Guid id, string returnUrl = "")
        {
            var rating = await _dbcontext.Ratings.FindAsync(id);
            if (rating == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Không tìm thấy đánh giá." });
                return NotFound();
            }

            rating.Status = rating.Status == 0 ? (byte)1 : (byte)0;
            rating.UpdatedAt = DateTime.UtcNow;
            await _dbcontext.SaveChangesAsync();

            await RecalcStoryRating(rating.StoryId);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true, newStatus = rating.Status });

            TempData["Success"] = rating.Status == 0 ? "Đã hiện đánh giá." : "Đã ẩn đánh giá.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        // =====================================================================
        // API — Chart / Stats
        // =====================================================================

        [HttpGet]
        public async Task<IActionResult> ChartByStar()
        {
            var data = await _dbcontext.Ratings
                .Where(r => r.Status == 0)
                .GroupBy(r => r.Score)
                .Select(g => new { score = g.Key, count = g.Count() })
                .OrderBy(x => x.score)
                .ToListAsync();

            var result = Enumerable.Range(1, 5).Select(s => new
            {
                score = s,
                count = data.FirstOrDefault(x => x.score == s)?.count ?? 0
            });

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> ChartTopRated()
        {
            var data = await _dbcontext.Ratings
                .Where(r => r.Status == 0)
                .GroupBy(r => new { r.StoryId, r.Story.Title })
                .Select(g => new
                {
                    title = g.Key.Title,
                    avg = Math.Round(g.Average(x => (double)x.Score), 2),
                    count = g.Count()
                })
                .Where(x => x.count >= 1)
                .OrderByDescending(x => x.avg)
                .Take(5)
                .ToListAsync();

            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> Stats()
        {
            var today = DateTime.UtcNow.Date;
            var stats = new
            {
                total = await _dbcontext.Ratings.CountAsync(),
                visible = await _dbcontext.Ratings.CountAsync(r => r.Status == 0),
                hidden = await _dbcontext.Ratings.CountAsync(r => r.Status == 1),
                todayCount = await _dbcontext.Ratings.CountAsync(r => r.CreatedAt >= today),
                avgScore = Math.Round(
                    await _dbcontext.Ratings.AverageAsync(r => (double?)r.Score) ?? 0, 2),
                total5Star = await _dbcontext.Ratings.CountAsync(r => r.Score == 5),
                total1Star = await _dbcontext.Ratings.CountAsync(r => r.Score == 1),
            };
            return Json(stats);
        }

        // =====================================================================
        // PRIVATE HELPER
        // =====================================================================
        private async Task RecalcStoryRating(Guid storyId)
        {
            var story = await _dbcontext.Stories.FindAsync(storyId);
            if (story == null) return;

            var ratingList = await _dbcontext.Ratings
                .Where(r => r.StoryId == storyId && r.Status == 0)
                .ToListAsync();

            story.RatingCount = ratingList.Count;
            story.RatingAvg = ratingList.Count > 0
                ? (decimal)Math.Round(ratingList.Average(r => (double)r.Score), 2)
                : 0;

            _dbcontext.Stories.Update(story);
            await _dbcontext.SaveChangesAsync();
        }
    }
}