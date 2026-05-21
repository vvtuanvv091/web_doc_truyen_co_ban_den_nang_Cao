using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using web_doc_truyen_Co_ban.Data;

namespace web_doc_truyen_Co_ban.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ErrorLogController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;
        public ErrorLogController(ApplicationDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        // ── INDEX — danh sách log, filter + phân trang ───────────────────────────
        public IActionResult Index(
            string? search,     
            string? action,      
            string? entityType,  
            string? status,      
            string? from,        
            string? to,          
            int page = 1)
        {
            ViewData["ActiveMenu"] = "ErrorLog";

            var query = _dbcontext.ErrorsAdmin
                .Include(x => x.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(x =>
                    x.User != null && x.User.Username.ToLower().Contains(search)
                    || x.ErrorMessage != null && x.ErrorMessage.ToLower().Contains(search)
                );
            }
        

            // Lọc theo hành động (Bookmark, Follow, Comment...)
            if (!string.IsNullOrWhiteSpace(action))
                query = query.Where(x => x.Action == action);

            // Lọc theo model bị tác động (Story, Chapter...)
            if (!string.IsNullOrWhiteSpace(entityType))
                query = query.Where(x => x.EntityType == entityType);

            // Lọc theo kết quả
            if (status == "success") query = query.Where(x => x.IsSuccess);
            if (status == "error") query = query.Where(x => !x.IsSuccess);

            // Lọc theo khoảng ngày
            if (DateTime.TryParse(from, out var fromDate))
                query = query.Where(x => x.CreatedAt >= fromDate);
            if (DateTime.TryParse(to, out var toDate))
                query = query.Where(x => x.CreatedAt <= toDate.AddDays(1));

            // ── STAT BAR ────────────────────────────────────────────────────────
            ViewBag.TotalLogs = _dbcontext.ErrorsAdmin.Count();
            ViewBag.TotalErrors = _dbcontext.ErrorsAdmin.Count(x => !x.IsSuccess);
            ViewBag.TotalUsers = _dbcontext.ErrorsAdmin.Select(x => x.UserId).Distinct().Count();
            ViewBag.TodayLogs = _dbcontext.ErrorsAdmin
                                      .Count(x => x.CreatedAt.Date == DateTime.UtcNow.Date);

            // ── DROPDOWN filter ──────────────────────────────────────────────────
            // Lấy danh sách Action distinct để hiện dropdown
            ViewBag.Actions = _dbcontext.ErrorsAdmin
                                     .Select(x => x.Action)
                                     .Distinct()
                                     .OrderBy(x => x)
                                     .ToList();

            // Lấy danh sách EntityType distinct để hiện dropdown
            ViewBag.EntityTypes = _dbcontext.ErrorsAdmin
                                     .Where(x => x.EntityType != null)
                                     .Select(x => x.EntityType)
                                     .Distinct()
                                     .OrderBy(x => x)
                                     .ToList();

            // ── PHÂN TRANG ───────────────────────────────────────────────────────
            int pageSize = 20;
            int total = query.Count();
            var logs = query
                             .OrderByDescending(x => x.CreatedAt)
                             .Skip((page - 1) * pageSize)
                             .Take(pageSize)
                             .ToList();

            ViewBag.Total = total;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

            // Giữ lại giá trị filter để hiện lại trên form
            ViewData["Search"] = search;
            ViewData["Action"] = action;
            ViewData["EntityType"] = entityType;
            ViewData["Status"] = status;
            ViewData["From"] = from;
            ViewData["To"] = to;

            return View(logs);
        }

        // ── CHART — hành động nhiều nhất ─────────────────────────────────────────
        public JsonResult ChartTopActions()
        {
            var data = _dbcontext.ErrorsAdmin
                .GroupBy(x => x.Action)
                .Select(g => new { action = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .Take(10)
                .ToList();

            return Json(data);
        }

        // ── CHART — top user hoạt động nhiều ─────────────────────────────────────
        public JsonResult ChartTopUsers()
        {
            var data = _dbcontext.ErrorsAdmin
                .Include(x => x.User)
                .Where(x => x.UserId != null)
                .GroupBy(x => new { x.UserId, x.User.Username })
                .Select(g => new { username = g.Key.Username, count = g.Count() })
                .OrderByDescending(x => x.count)
                .Take(10)
                .ToList();

            return Json(data);
        }

        // ── CHART — xu hướng log 30 ngày ─────────────────────────────────────────
        public JsonResult ChartTrend()
        {
            var from = DateTime.UtcNow.AddDays(-30);

            var data = _dbcontext.ErrorsAdmin
                .Where(x => x.CreatedAt >= from)
                .GroupBy(x => x.CreatedAt.Date)
                .Select(g => new { date = g.Key, count = g.Count() })
                .OrderBy(x => x.date)
                .ToList();

            return Json(data);
        }

        // ── CHART — tỉ lệ thành công / lỗi ──────────────────────────────────────
        public JsonResult ChartSuccessRate()
        {
            var success = _dbcontext.ErrorsAdmin    .Count(x => x.IsSuccess);
            var error = _dbcontext.ErrorsAdmin.Count(x => !x.IsSuccess);

            return Json(new[]
            {
                new { label = "Thành công", count = success },
                new { label = "Thất bại",   count = error   }
            });
        }

        // ── CHART — giờ cao điểm ──────────────────────────────────────────────────
        public JsonResult ChartHeatmap()
        {
            var data = _dbcontext.ErrorsAdmin
                .GroupBy(x => x.CreatedAt.Hour)
                .Select(g => new { hour = g.Key, count = g.Count() })
                .OrderBy(x => x.hour)
                .ToList();

            return Json(data);
        }

        // ── XÓA 1 LOG ────────────────────────────────────────────────────────────
        [HttpPost]
        public IActionResult Delete(Guid id)
        {
            var log = _dbcontext.ErrorsAdmin.Find(id);
            if (log == null) return NotFound();

            _dbcontext.ErrorsAdmin.Remove(log);
            _dbcontext.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        // ── XÓA TOÀN BỘ LOG CŨ HƠN 30 NGÀY ─────────────────────────────────────
        [HttpPost]
        public IActionResult DeleteOld()
        {
            var cutoff = DateTime.UtcNow.AddDays(-30);
            var oldLogs = _dbcontext.ErrorsAdmin.Where(x => x.CreatedAt < cutoff).ToList();

            _dbcontext.ErrorsAdmin.RemoveRange(oldLogs);
            _dbcontext.SaveChanges();

            return RedirectToAction(nameof(Index));
        }
    }
}
