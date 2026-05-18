using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Models;

namespace web_doc_truyen_Co_ban.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ChapterCommentController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;

        public ChapterCommentController(ApplicationDbContext context)
        {
            _dbcontext = context;
        }

        // =====================================================================
        // INDEX - Danh sách bình luận chương + thống kê
        // =====================================================================
        public async Task<IActionResult> Index(string? search, string? storyId, string? chapterId, int? status, int page = 1)
        {
            ViewData["ActiveMenu"] = "ChapterComment";
            int pageSize = 15;

            var query = _dbcontext.ChapterComments
                .Include(c => c.User)
                .Include(c => c.Chapter)
                    .ThenInclude(ch => ch.Story)
                .AsQueryable();

            // Lọc theo từ khóa
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(c =>
                    c.Content.ToLower().Contains(search) ||
                    c.User.Username.ToLower().Contains(search));
                ViewData["Search"] = search;
            }

            // Lọc theo truyện
            if (!string.IsNullOrWhiteSpace(storyId) && Guid.TryParse(storyId, out var sid))
            {
                query = query.Where(c => c.StoryId == sid);
                ViewData["StoryId"] = storyId;
            }

            // Lọc theo chương
            if (!string.IsNullOrWhiteSpace(chapterId) && Guid.TryParse(chapterId, out var cid))
            {
                query = query.Where(c => c.ChapterId == cid);
                ViewData["ChapterId"] = chapterId;
            }

            // Lọc theo trạng thái
            if (status.HasValue)
            {
                query = query.Where(c => c.Status == (byte)status.Value);
                ViewData["Status"] = status;
            }

            int total = await query.CountAsync();

            var comments = await query
                .Include(c => c.User)
                .Include(c => c.Chapter)
                    .ThenInclude(ch => ch.Story)
                .Include(c => c.Replies)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Thống kê
            ViewBag.TotalAll = await _dbcontext.ChapterComments.CountAsync();
            ViewBag.TotalVisible = await _dbcontext.ChapterComments.CountAsync(c => c.Status == 0);
            ViewBag.TotalHidden = await _dbcontext.ChapterComments.CountAsync(c => c.Status == 1);
            ViewBag.TotalDeleted = await _dbcontext.ChapterComments.CountAsync(c => c.Status == 2);

            // Dropdown truyện + chương cho bộ lọc
            ViewBag.Stories = await _dbcontext.Stories
                .Select(s => new { s.Id, s.Title })
                .ToListAsync();

            ViewBag.Chapters = await _dbcontext.Chapters
                .Select(ch => new { ch.Id, ch.Title, ch.StoryId })
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

            return View(comments);
        }

        // =====================================================================
        // DETAIL — xem bình luận gốc + toàn bộ replies
        // =====================================================================
        public async Task<IActionResult> Detail(Guid id)
        {
            ViewData["ActiveMenu"] = "ChapterComment";

            var comment = await _dbcontext.ChapterComments
                .Include(c => c.User)
                .Include(c => c.Chapter)
                    .ThenInclude(ch => ch.Story)
                .Include(c => c.Parent)
                .Include(c => c.Replies.Where(r => r.Status != 2))
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null) return NotFound();

            return View(comment);
        }

        // =====================================================================
        // EDIT
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            ViewData["ActiveMenu"] = "ChapterComment";

            var comment = await _dbcontext.ChapterComments
                .Include(c => c.User)
                .Include(c => c.Chapter)
                    .ThenInclude(ch => ch.Story)
                .Include(c => c.Replies.Where(r => r.Status != 2))
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null) return NotFound();

            return  View(comment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, ChapterCommentModel model)
        {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            var comment = await _dbcontext.ChapterComments.FindAsync(id);
            if (comment == null) return NotFound();

            comment.Content = model.Content;
            comment.Status = model.Status;
            comment.UpdatedAt = DateTime.UtcNow;

            _dbcontext.ChapterComments.Update(comment);
            await _dbcontext.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật bình luận.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================================
        // DELETE (soft delete — Status = 2, kéo theo replies)
        public async Task<IActionResult> Delete(int id)
        {
            var comment = await _dbcontext.ChapterComments.FindAsync(id);

            if (comment == null)
                return NotFound();

            return View(comment);
        }

        [HttpPost, ActionName("Admin/Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var comment = await _dbcontext.ChapterComments.FindAsync(id);
            if (comment == null) return NotFound();

            comment.Status = 2;
            comment.UpdatedAt = DateTime.UtcNow;

            // Soft delete toàn bộ replies
            var replies = await _dbcontext.ChapterComments
                .Where(c => c.ParentId == id)
                .ToListAsync();

            foreach (var reply in replies)
            {
                reply.Status = 2;
                reply.UpdatedAt = DateTime.UtcNow;
            }

            await _dbcontext.SaveChangesAsync();

            TempData["Success"] = "Đã xóa bình luận.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================================
        // TOGGLE HIDE / SHOW  (hỗ trợ cả AJAX và redirect thường)
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(Guid id, string returnUrl = "")
        {
            var comment = await _dbcontext.ChapterComments.FindAsync(id);
            if (comment == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Không tìm thấy bình luận." });
                return NotFound();
            }

            comment.Status = comment.Status == 0 ? (byte)1 : (byte)0;
            comment.UpdatedAt = DateTime.UtcNow;
            await _dbcontext.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true, newStatus = comment.Status });

            TempData["Success"] = comment.Status == 0 ? "Đã hiện bình luận." : "Đã ẩn bình luận.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        // =====================================================================
        // API — Chart data (JSON endpoints cho dashboard)
        // =====================================================================

        /// <summary>Số bình luận chương 7 ngày gần nhất</summary>
        [HttpGet]
        public async Task<IActionResult> ChartWeekly()
        {
            var now = DateTime.UtcNow.Date;
            var start = now.AddDays(-6);

            var data = await _dbcontext.ChapterComments
                .Where(c => c.CreatedAt >= start && c.Status != 2)
                .GroupBy(c => c.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var result = Enumerable.Range(0, 7).Select(i =>
            {
                var d = start.AddDays(i);
                return new
                {
                    date = d.ToString("dd/MM"),
                    count = data.FirstOrDefault(x => x.Date == d)?.Count ?? 0
                };
            });

            return Json(result);
        }

        /// <summary>Top 5 chương nhiều bình luận nhất</summary>
        [HttpGet]
        public async Task<IActionResult> ChartTopChapters()
        {
            var data = await _dbcontext.ChapterComments
                .Where(c => c.Status == 0)
                .GroupBy(c => new { c.ChapterId, c.Chapter.Title })
                .Select(g => new { title = g.Key.Title, count = g.Count() })
                .OrderByDescending(x => x.count)
                .Take(5)
                .ToListAsync();

            return Json(data);
        }

        /// <summary>Thống kê tổng quan cho metric cards</summary>
        [HttpGet]
        public async Task<IActionResult> Stats()
        {
            var today = DateTime.UtcNow.Date;
            var stats = new
            {
                total = await _dbcontext.ChapterComments.CountAsync(),
                visible = await _dbcontext.ChapterComments.CountAsync(c => c.Status == 0),
                hidden = await _dbcontext.ChapterComments.CountAsync(c => c.Status == 1),
                deleted = await _dbcontext.ChapterComments.CountAsync(c => c.Status == 2),
                todayCount = await _dbcontext.ChapterComments.CountAsync(c => c.CreatedAt >= today)
            };
            return Json(stats);
        }
    }
}