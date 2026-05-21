using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Models;

namespace web_doc_truyen_Co_ban.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class FollowController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;

        public FollowController(ApplicationDbContext db)
        {
            _dbcontext = db;
        }

        // =====================================================================
        // INDEX — danh sách theo dõi + thống kê + lọc + phân trang
        // =====================================================================
        public async Task<IActionResult> Index(string? search, Guid? storyId, byte? status, int page = 1)
        {
            ViewData["ActiveMenu"] = "Follow";
            int pageSize = 10;

            var query = _dbcontext.Follows
                .Include(f => f.User)
                .Include(f => f.Story)
                .AsQueryable();

            // Lọc theo từ khóa (tên truyện hoặc tên user)
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(f =>
                    f.Story.Title.ToLower().Contains(search) ||
                    f.User.Username.ToLower().Contains(search));
                ViewData["Search"] = search;
            }

            // Lọc theo truyện
            if (storyId.HasValue)
            {
                query = query.Where(f => f.StoryId == storyId.Value);
                ViewData["StoryId"] = storyId;
            }

            // Lọc theo trạng thái
            if (status.HasValue)
            {
                query = query.Where(f => f.Status == status.Value);
                ViewData["Status"] = status;
            }

            // Thống kê cho ViewBag (giống trang Bình luận)
            ViewBag.TotalAll = await _dbcontext.Follows.CountAsync();
            ViewBag.TotalVisible = await _dbcontext.Follows.CountAsync(f => f.Status == 0);
            ViewBag.TotalHidden = await _dbcontext.Follows.CountAsync(f => f.Status == 1);
            ViewBag.TotalDeleted = await _dbcontext.Follows.CountAsync(f => f.Status == 2);

            // Top truyện được follow nhiều nhất (dùng cho widget "Hot nhất")
            ViewBag.TopStories = await _dbcontext.Follows
                .Where(f => f.Status != 2)
                .GroupBy(f => new { f.StoryId, f.Story.Title })
                .Select(g => new
                {
                    StoryId = g.Key.StoryId,
                    Title = g.Key.Title,
                    FollowCount = g.Count()
                })
                .OrderByDescending(x => x.FollowCount)
                .Take(5)
                .ToListAsync();

            // Dữ liệu biểu đồ 7 ngày qua
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7).Date;
            ViewBag.ChartData = await _dbcontext.Follows
                .Where(f => f.CreatedAt >= sevenDaysAgo)
                .GroupBy(f => f.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToListAsync();

            // Phân trang
            int total = await query.CountAsync();

            var follows = await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Danh sách truyện cho dropdown lọc
            ViewBag.Stories = await _dbcontext.Stories
                .OrderBy(s => s.Title)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

            return View(follows);
        }

        // =====================================================================
        // EDIT GET
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            ViewData["ActiveMenu"] = "Follow";

            var follow = await _dbcontext.Follows
                .Include(f => f.User)
                .Include(f => f.Story)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (follow == null) return NotFound();

            ViewBag.Stories = await _dbcontext.Stories.OrderBy(s => s.Title).ToListAsync();
            return View(follow);
        }

        // =====================================================================
        // EDIT POST — chỉ cho phép sửa Status
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, FollowModel model)
        {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Stories = await _dbcontext.Stories.OrderBy(s => s.Title).ToListAsync();
                return View(model);
            }

            var follow = await _dbcontext.Follows.FindAsync(id);
            if (follow == null) return NotFound();

            follow.Status = model.Status;
            // Không cập nhật CreatedAt khi edit — giữ nguyên thời gian tạo ban đầu

            _dbcontext.Follows.Update(follow);
            await _dbcontext.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật theo dõi.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================================
        // DELETE GET — xác nhận xóa
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            ViewData["ActiveMenu"] = "Follow";

            var follow = await _dbcontext.Follows
                .Include(f => f.User)
                .Include(f => f.Story)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (follow == null) return NotFound();
            return View(follow);
        }

        // =====================================================================
        // DELETE POST — soft delete (Status = 2)
        // =====================================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var follow = await _dbcontext.Follows.FindAsync(id);
            if (follow == null) return NotFound();

            follow.Status = 2; // Soft delete
            await _dbcontext.SaveChangesAsync();

            TempData["Success"] = "Đã xóa theo dõi.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================================
        // TOGGLE HIDE / SHOW — hỗ trợ cả AJAX lẫn redirect thường
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(Guid id, string returnUrl = "")
        {
            var follow = await _dbcontext.Follows.FindAsync(id);
            if (follow == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Không tìm thấy theo dõi." });
                return NotFound();
            }

            // Chỉ toggle giữa 0 (hiện) và 1 (ẩn); bỏ qua nếu đã bị xóa mềm (2)
            if (follow.Status == 2)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Theo dõi đã bị xóa, không thể thay đổi trạng thái." });
                TempData["Error"] = "Theo dõi đã bị xóa.";
                return RedirectToAction(nameof(Index));
            }

            follow.Status = follow.Status == 0 ? (byte)1 : (byte)0;
            await _dbcontext.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true, newStatus = follow.Status });

            TempData["Success"] = follow.Status == 0 ? "Đã hiện theo dõi." : "Đã ẩn theo dõi.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        // =====================================================================
        // DETAILS — xem chi tiết (tuỳ chọn, dùng cho nút 👁 nếu cần)
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            ViewData["ActiveMenu"] = "Follow";

            var follow = await _dbcontext.Follows
                .Include(f => f.User)
                .Include(f => f.Story)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (follow == null) return NotFound();
            return View(follow);
        }
        [HttpGet]
        public async Task<IActionResult> ChartWeekly()
        {
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-6).Date;

            // Tạo danh sách đủ 7 ngày kể cả ngày 0 follow
            var raw = await _dbcontext.Follows
                .Where(f => f.CreatedAt.Date >= sevenDaysAgo)
                .GroupBy(f => f.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var result = Enumerable.Range(0, 7)
                .Select(i => sevenDaysAgo.AddDays(i))
                .Select(d => new
                {
                    date = d.ToString("dd/MM"),
                    count = raw.FirstOrDefault(r => r.Date == d)?.Count ?? 0
                })
                .ToList();

            return Json(result);
        }

        // GET /Admin/Follow/ChartTopStories
        // Trả về top 5 truyện được follow nhiều nhất
        [HttpGet]
        public async Task<IActionResult> ChartTopStories()
        {
            var result = await _dbcontext.Follows
                .Where(f => f.Status != 2)
                .GroupBy(f => new { f.StoryId, f.Story.Title })
                .Select(g => new
                {
                    title = g.Key.Title,
                    count = g.Count()
                })
                .OrderByDescending(x => x.count)
                .Take(5)
                .ToListAsync();

            return Json(result);
        }

        // GET /Admin/Follow/ChartTopUsers
        // Trả về top 5 người dùng follow nhiều truyện nhất
        [HttpGet]
        public async Task<IActionResult> ChartTopUsers()
        {
            var result = await _dbcontext.Follows
                .Where(f => f.Status != 2)
                .GroupBy(f => new { f.UserId, f.User.Username })
                .Select(g => new
                {
                    username = g.Key.Username,
                    count = g.Count()
                })
                .OrderByDescending(x => x.count)
                .Take(5)
                .ToListAsync();

            return Json(result);
        }
    }
}