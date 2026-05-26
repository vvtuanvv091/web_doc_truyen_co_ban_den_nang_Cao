using DocumentFormat.OpenXml.Vml;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Hubs;
using web_doc_truyen_Co_ban.Models;

namespace web_doc_truyen_Co_ban.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ThongBaoController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;
        private readonly IHubContext<NotificationHub> _hub;

        private static readonly string[] AdminTypes = { "new_user", "login", "report", "new_story", "new_chapter" };

        public ThongBaoController(ApplicationDbContext dbcontext, IHubContext<NotificationHub> hub)
        {
            _dbcontext = dbcontext;
            _hub = hub;
        }

        private Guid? GetCurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : null;
        }

        // ── Trang Index ────────────────────────────────────────
        public async Task<IActionResult> Index(string? keyword ,int page)
        {
            ViewData["ActiveMenu"] = "ThongBao";//Notification lười
            int pageSize = 10;
            var userId = GetCurrentUserId();
            var query= _dbcontext.ThongBaos.AsQueryable();
            if (userId == null) return RedirectToAction("Login", "Account");
            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.Trim().ToLower();
                //chưa biết tìm theo j bởi vì o cần thiets dạng thông báo này
                query=query.Where(c=>c.User.DisplayName.ToLower().Contains(keyword));
                ViewData["Search"] = keyword;
            }
            int total = await query.CountAsync();
            var list = await _dbcontext.ThongBaos
                .Where(t => t.UserId == userId && AdminTypes.Contains(t.Type))
                .OrderByDescending(t => t.CreatedAt)
                .Include(t => t.Sender)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
            return View(list);
        }

        // ── Đánh dấu đã đọc 1 cái ─────────────────────────────
        [HttpPost]
        public async Task<IActionResult> DanhDauDaDoc(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var tb = await _dbcontext.ThongBaos
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (tb == null) return NotFound();

            tb.IsRead = true;
            await _dbcontext.SaveChangesAsync();

            return Ok();
        }

        // ── Đánh dấu đọc tất cả ───────────────────────────────
        [HttpPost]
        public async Task<IActionResult> DanhDauTatCaDaDoc()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var chuaDoc = await _dbcontext.ThongBaos
                .Where(t => t.UserId == userId && !t.IsRead && AdminTypes.Contains(t.Type))
                .ToListAsync();

            chuaDoc.ForEach(t => t.IsRead = true);
            await _dbcontext.SaveChangesAsync();

            await _hub.Clients
                .Group($"user_{userId}")
                .SendAsync("CapNhatSoThongBao", 0);

            return Ok();
        }
    }
}