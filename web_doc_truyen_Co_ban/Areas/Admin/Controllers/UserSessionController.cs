using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UAParser;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Models;

namespace web_doc_truyen_Co_ban.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UserSessionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserSessionController(ApplicationDbContext context)
        {
            _context = context;
        }
        // GET: /UserSession/Index
        public async Task<IActionResult> Index(
            Guid? userId = null,
            string? search = null,
            string? deviceType = null,
            string? status = null,
            int page = 1,
            int pageSize = 20)
        {
            ViewData["ActiveMenu"] = "UserSession";
            var query = _context.UsersSessions
                .Include(s => s.User)
                .AsQueryable();

            // Lọc theo userId (từ trang User Management bấm vào)
            if (userId.HasValue)
                query = query.Where(s => s.UserId == userId.Value);

            // Tìm kiếm theo username / email / ip
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(s =>
                    (s.User != null && s.User.Username.ToLower().Contains(search)) ||
                    (s.User != null && s.User.Email.ToLower().Contains(search)) ||
                    (s.IpAddress != null && s.IpAddress.Contains(search)));
            }

            // Lọc theo trạng thái
            var now = DateTime.UtcNow;
            if (status == "active")
                query = query.Where(s => s.ExpiresAt > now);
            else if (status == "expired")
                query = query.Where(s => s.ExpiresAt <= now);

            var totalCount = await query.CountAsync();

            var sessions = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Map sang ViewModel + parse UserAgent
            var viewModels = sessions.Select(s =>
            {
                var vm = new UserSessionViewModel
                {
                    Id = s.Id,
                    UserId = s.UserId,
                    Username = s.User?.Username ?? "—",
                    Email = s.User?.Email ?? "—",
                    IpAddress = s.IpAddress ?? "—",
                    UserAgent = s.UserAgent,
                    ExpiresAt = s.ExpiresAt,
                    CreatedAt = s.CreatedAt,
                    IsExpired = s.ExpiresAt < now,
                    DeviceInfo = ParseUserAgent(s.UserAgent)
                };
                return vm;
            }).ToList();

            // Lọc deviceType sau khi parse (vì parse xảy ra in-memory)
            if (!string.IsNullOrWhiteSpace(deviceType))
                viewModels = viewModels
                    .Where(v => v.DeviceInfo.DeviceType == deviceType)
                    .ToList();

            ViewBag.TotalCount = totalCount;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.UserId = userId;
            ViewBag.Search = search;
            ViewBag.DeviceFilter = deviceType;
            ViewBag.StatusFilter = status;

            return View(viewModels);
        }

        // GET: /UserSession/Detail/{id}
        public async Task<IActionResult> Detail(Guid id)
        {
            var session = await _context.UsersSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null) return NotFound();

            var vm = new UserSessionViewModel
            {
                Id = session.Id,
                UserId = session.UserId,
                Username = session.User?.Username ?? "—",
                Email = session.User?.Email ?? "—",
                IpAddress = session.IpAddress ?? "—",
                UserAgent = session.UserAgent ?? "—",
                ExpiresAt = session.ExpiresAt,
                CreatedAt = session.CreatedAt,
                IsExpired = session.ExpiresAt < DateTime.UtcNow,
                DeviceInfo = ParseUserAgent(session.UserAgent)
            };

            return View(vm);
        }

        // POST: /UserSession/Revoke/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Revoke(Guid id)
        {
            var session = await _context.UsersSessions.FindAsync(id);
            if (session != null)
            {
                _context.UsersSessions.Remove(session);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã thu hồi phiên đăng nhập.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /UserSession/RevokeAllByUser/{userId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeAllByUser(Guid userId)
        {
            var sessions = _context.UsersSessions;
            _context.UsersSessions.RemoveRange(sessions);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã thu hồi tất cả phiên của người dùng.";
            return RedirectToAction(nameof(Index));
        }

        // ----------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------
        private static DeviceInfoViewModel ParseUserAgent(string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
                return new DeviceInfoViewModel { Raw = "—" };

            var uaParser = Parser.GetDefault();
            var clientInfo = uaParser.Parse(userAgent);

            return new DeviceInfoViewModel
            {
                Browser = $"{clientInfo.UA.Family} {clientInfo.UA.Major}".Trim(),
                Os = $"{clientInfo.OS.Family} {clientInfo.OS.Major}".Trim(),
                DeviceFamily = clientInfo.Device.Family ?? "Unknown",
                DeviceType = DetectDeviceType(userAgent),
                Raw = userAgent
            };
        }

        private static string DetectDeviceType(string ua)
        {
            ua = ua.ToLower();
            if (ua.Contains("iphone") || (ua.Contains("android") && ua.Contains("mobile")))
                return "mobile";
            if (ua.Contains("ipad") || ua.Contains("tablet"))
                return "tablet";
            if (ua.Contains("bot") || ua.Contains("crawler"))
                return "bot";
            return "desktop";
        }

    }

}