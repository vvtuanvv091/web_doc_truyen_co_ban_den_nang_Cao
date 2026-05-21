using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Hubs;
using web_doc_truyen_Co_ban.Models;

namespace web_doc_truyen_Co_ban.Controllers
{
    //chạy realtime nên phải có hubcontext để push thông báo mới về client
    public class ThongBaoController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;
        private readonly IHubContext<NotificationHub> _hub;
        public ThongBaoController(ApplicationDbContext dbcontext, IHubContext<NotificationHub> hub)
        {
            _dbcontext = dbcontext;
            _hub = hub;
        }
        //lấy user đăng nhập//này o lưu dc session
        private Guid? GetCurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
        //lấy nó ra 1 trang nhét vàopip
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var list = await _dbcontext.ThongBaos
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Include(t => t.Sender)
                .ToListAsync();

            return View(list);
        }
        //trả jsson để scripts lấy
        [HttpGet]
        public async Task<IActionResult> SoThongBaoChuaDoc()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Json(new { count = 0 });

            var count = await _dbcontext.ThongBaos
                .CountAsync(t => t.UserId == userId && !t.IsRead);

            return Json(new { count });
        }
        //đã dodcj lất id
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
        //đánh dấu dã đọc tất cả
        [HttpPost]
        public async Task<IActionResult> DanhDauTatCaDaDoc()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var chuaDoc = await _dbcontext.ThongBaos
                .Where(t => t.UserId == userId && !t.IsRead)
                .ToListAsync();

            chuaDoc.ForEach(t => t.IsRead = true);
            await _dbcontext.SaveChangesAsync();

            // Đẩy realtime: reset chuông về 0
            await _hub.Clients
                .Group($"user_{userId}")
                .SendAsync("CapNhatSoThongBao", 0);

            return Ok();
        }
        /// <summary>
        /// Tạo thông báo khi có reply comment
        /// </summary>


        public async Task PushThongBaoReply(
            Guid userNhanId,
            Guid senderid,
            string tenSender,
            string linkComment)
        {
            var tb = new ThongBaoModel
            {
                UserId = userNhanId,
                SenderId = senderid,
                Type = "reply",
                Message = $"{tenSender} đã trả lời bình luận của bạn.",
                Link = linkComment
            };

            _dbcontext.ThongBaos.Add(tb);
            await _dbcontext.SaveChangesAsync();

            var soMoi = await _dbcontext.ThongBaos
                .CountAsync(t => t.UserId == userNhanId && !t.IsRead);

            // Push realtime đến đúng user
            await _hub.Clients
                .Group($"user_{userNhanId}")
                .SendAsync("NhanThongBaoMoi", new
                {
                    id = tb.Id,
                    type = tb.Type,
                    message = tb.Message,
                    link = tb.Link,
                    time = tb.CreatedAt.ToString("HH:mm dd/MM/yyyy"),
                    soMoi
                });
        }

        /// <summary>
        /// Tạo thông báo khi truyện đang theo dõi có chương mới
        /// </summary>
        public async Task PushThongBaoTruyenMoi(
            IEnumerable<Guid> danhSachNguoiTheoDoi,
            string tenTruyen,
            string tenChuong,
            string linkChuong)
        {
            var danhSachTb = danhSachNguoiTheoDoi.Select(uid => new ThongBaoModel
            {
                UserId = uid,
                Type = "new_chapter",
                Message = $"Truyện \"{tenTruyen}\" vừa ra chương mới: {tenChuong}",
                Link = linkChuong
            }).ToList();

            _dbcontext.ThongBaos.AddRange(danhSachTb);
            await _dbcontext.SaveChangesAsync();

            // Push song song đến tất cả người theo dõi
            var tasks = danhSachNguoiTheoDoi.Select(async uid =>
            {
                var soMoi = await _dbcontext.ThongBaos
                    .CountAsync(t => t.UserId == uid && !t.IsRead);

                await _hub.Clients
                    .Group($"user_{uid}")
                    .SendAsync("NhanThongBaoMoi", new
                    {
                        type = "new_chapter",
                        message = $"Truyện \"{tenTruyen}\" vừa ra chương mới: {tenChuong}",
                        link = linkChuong,
                        time = DateTime.UtcNow.ToString("HH:mm dd/MM/yyyy"),
                        soMoi
                    });
            });

            await Task.WhenAll(tasks);
        }

        // Trả danh sách 20 thông báo mới nhất dạng JSON cho dropdown
        [HttpGet]
        public async Task<IActionResult> DanhSach()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Json(new List<object>());

            var list = await _dbcontext.ThongBaos
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(20)
                .Select(t => new
                {
                    id = t.Id,
                    type = t.Type,
                    message = t.Message,
                    link = t.Link,
                    isRead = t.IsRead,
                    time = t.CreatedAt.ToString("HH:mm dd/MM/yyyy")
                })
                .ToListAsync();

            return Json(list);
        }

    }
}
