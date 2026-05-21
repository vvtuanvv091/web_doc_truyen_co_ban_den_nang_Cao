using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Models;

namespace web_doc_truyen_Co_ban.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class StoryCommentController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;
        
        public StoryCommentController(ApplicationDbContext context)
        {
            _dbcontext = context;
        }

        // =====================================================================
        // INDEX - Danh sách bình luận + thống kê
        // =====================================================================
        public async Task<IActionResult> Index(string? search, string? storyId, int? status, int page = 1)
        {
            ViewData["ActiveMenu"] = "StoryComment";
            int pageSize = 15;

            var query = _dbcontext.StoryComments
                .Include(c => c.User)
                .Include(c => c.Story)
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

            // Lọc theo trạng thái
            if (status.HasValue)
            {
                query = query.Where(c => c.Status == (byte)status.Value);
                ViewData["Status"] = status;
            }

            int total = await query.CountAsync();

            var comments = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new StoryCommentModel
                {
                    Id = c.Id,
                    StoryId = c.StoryId,
                    StoryTitle = c.Story.Title,
                    UserId = c.UserId,
                    UserName = c.User.Username,
                    ParentId = c.ParentId,
                    Content = c.Content,
                    LikeCount = c.LikeCount,
                    Status = c.Status,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    ReplyCount = c.Replies.Count(r => r.Status != 2)
                })
                .ToListAsync();

            // Thống kê
            ViewBag.TotalAll = await _dbcontext.StoryComments.CountAsync();
            ViewBag.TotalVisible = await _dbcontext.StoryComments.CountAsync(c => c.Status == 0);
            ViewBag.TotalHidden = await _dbcontext.StoryComments.CountAsync(c => c.Status == 1);
            ViewBag.TotalDeleted = await _dbcontext.StoryComments.CountAsync(c => c.Status == 2);

            // Dropdown truyện cho bộ lọc
            ViewBag.Stories = await _dbcontext.Stories
                .Select(s => new { s.Id, s.Title })
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

            return View(comments);
        }

        // =====================================================================
        // CREATE
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewData["ActiveMenu"] = "StoryComment";
            ViewBag.Stories = await _dbcontext.Stories.ToListAsync();
            ViewBag.Users = await _dbcontext.Users.ToListAsync();
            return View(new StoryCommentModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StoryCommentModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Stories = await _dbcontext.Stories.ToListAsync();
                ViewBag.Users = await _dbcontext.Users.ToListAsync();
                return View(model);
            }

            // Kiểm tra ParentId hợp lệ
            if (model.ParentId.HasValue)
            {
                bool parentExists = await _dbcontext.StoryComments
                    .AnyAsync(c => c.Id == model.ParentId.Value && c.StoryId == model.StoryId);
                if (!parentExists)
                {
                    ModelState.AddModelError("ParentId", "Bình luận cha không tồn tại hoặc không thuộc truyện này.");
                    ViewBag.Stories = await _dbcontext.Stories.ToListAsync();
                    ViewBag.Users = await _dbcontext.Users.ToListAsync();
                    return View(model);
                }
            }

            model.Id = Guid.NewGuid();
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;
            model.LikeCount = 0;
            model.Status = 0;

            _dbcontext.StoryComments.Add(model);
            await _dbcontext.SaveChangesAsync();

            TempData["Success"] = "Đã thêm bình luận thành công.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================================
        // EDIT
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            ViewData["ActiveMenu"] = "StoryComment";

            var comment = await _dbcontext.StoryComments
                .Include(c => c.User)
                .Include(c => c.Story)
                .Include(c => c.Replies.Where(r => r.Status != 2))
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null) return NotFound();

            ViewBag.Stories = await _dbcontext.Stories.ToListAsync();
            return View(comment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, StoryCommentModel model)
        {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Stories = await _dbcontext.Stories.ToListAsync();
                return View(model);
            }

            var comment = await _dbcontext.StoryComments.FindAsync(id);
            if (comment == null) return NotFound();

            comment.Content = model.Content;
            comment.Status = model.Status;
            comment.UpdatedAt = DateTime.UtcNow;

            _dbcontext.StoryComments.Update(comment);
            await _dbcontext.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật bình luận.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================================
        // DELETE (soft delete — Status = 2)
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            ViewData["ActiveMenu"] = "StoryComment";

            var comment = await _dbcontext.StoryComments
                .Include(c => c.User)
                .Include(c => c.Story)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null) return NotFound();
            return View(comment);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var comment = await _dbcontext.StoryComments.FindAsync(id);
            if (comment == null) return NotFound();

            // Soft delete
            comment.Status = 2;
            comment.UpdatedAt = DateTime.UtcNow;

            // Soft delete tất cả reply con
            var replies = await _dbcontext.StoryComments
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
        // TOGGLE HIDE/SHOW (AJAX-friendly, cũng hoạt động như redirect)
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(Guid id, string returnUrl = "")
        {
            var comment = await _dbcontext.StoryComments.FindAsync(id);
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
        // DETAIL — xem toàn bộ bình luận + replies của một truyện
        // =====================================================================
        public async Task<IActionResult> Detail(Guid id)
        {
            ViewData["ActiveMenu"] = "StoryComment";

            var comment = await _dbcontext.StoryComments
                .Include(c => c.User)
                .Include(c => c.Story)
                .Include(c => c.Parent)
                .Include(c => c.Replies.Where(r => r.Status != 2))
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null) return NotFound();

            return View(comment);
        }

        // =====================================================================
        // API — Chart data (JSON endpoints cho dashboard)
        // =====================================================================

        /// <summary>Trả về số bình luận 7 ngày gần nhất</summary>
        [HttpGet]
        public async Task<IActionResult> ChartWeekly()
        {
            var now = DateTime.UtcNow.Date;
            var start = now.AddDays(-6);

            var data = await _dbcontext.StoryComments
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

        /// <summary>Top 5 truyện nhiều bình luận nhất</summary>
        [HttpGet]
        public async Task<IActionResult> ChartTopStories()
        {
            var data = await _dbcontext.StoryComments
                .Where(c => c.Status == 0)
                .GroupBy(c => new { c.StoryId, c.Story.Title })
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
                total = await _dbcontext.StoryComments.CountAsync(),
                visible = await _dbcontext.StoryComments.CountAsync(c => c.Status == 0),
                hidden = await _dbcontext.StoryComments.CountAsync(c => c.Status == 1),
                deleted = await _dbcontext.StoryComments.CountAsync(c => c.Status == 2),
                todayCount = await _dbcontext.StoryComments.CountAsync(c => c.CreatedAt >= today)
            };
            return Json(stats);
        }
    }

}