using LinqToDB.Async;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Hubs;
using web_doc_truyen_Co_ban.Models;

namespace web_doc_truyen_Co_ban.Controllers
{
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _dbcontex;
        private readonly IHubContext<NotificationHub> _hub;
        public ReportController(ApplicationDbContext dbcontex,IHubContext<NotificationHub>hub)
        {  _dbcontex = dbcontex; 
            _hub = hub;
        }
        public IActionResult Index()
        {
            return View();
        }
        //lấy userid đnăg nhập clamp
        private Guid? GetCurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw,out var id)?id:null;
        }
        [HttpGet]
        public async Task<IActionResult> LichSuBaoCao()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var list = await _dbcontex.Reports
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(list);
        }

        [HttpPost]
        public async Task<IActionResult>SendReport(string targetType, Guid targetId,string reason )//lysod reason
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Json(new { success = false, message = "Bạn cần đăng nhập!" });
            var loaiHopLe = new[] { "comment", "story", "chapter", "user" };
            if (!loaiHopLe.Contains(targetType))
                return Json(new { success = false, message = "Loại báo cáo không hợp lệ!" });
            if (string.IsNullOrWhiteSpace(reason))
                return Json(new { success = false, message = "Vui lòng nhập lý do báo cáo!" });
            var daGui = await _dbcontex.Reports.AnyAsync(b =>
               b.UserId == userId &&
               b.TargetType == targetType &&
               b.TargetId == targetId &&
               b.Status == "pending");
            if (daGui)
                return Json(new { success = false, message = "Bạn đã báo cáo nội dung này rồi!" });
            var bc = new ReportModel
            {
                UserId = userId.Value,
                TargetType = targetType,
                TargetId= targetId,
                Reason = reason
            };
            _dbcontex.Reports.Add(bc);
            await _dbcontex.SaveChangesAsync();

            // GUiwr đến Admin
            var danhSachAdminId = await _dbcontex.Users
                .Where(u => u.Role != null && u.Role.Name == "Quản Trị Viên")
                .Select(u => u.Id)
                .ToListAsync();
            if (danhSachAdminId.Any())
            {
                var link = targetType switch
                {
                    "story" => Url.Action("Edit", "Story", new { area = "Admin", id = targetId }),
                    "chapter" => Url.Action("Edit", "Chapter", new { area = "Admin", id = targetId }),
                    "user" => Url.Action("Edit", "User", new { area = "Admin", id = targetId }),
                    "comment" => Url.Action("Edit", "StoryComment", new { area = "Admin", id = targetId }),
                    _ => Url.Action("Index", "Report", new { area = "Admin" })
                };

                var danhSachTb = danhSachAdminId.Select(uid => new ThongBaoModel
                {
                    UserId = uid,
                    SenderId = userId,
                    Type = "report",
                    Message = $"Có báo cáo mới: [{targetType}] - \"{reason}\"",
                    Link = link
                }).ToList();

                _dbcontex.ThongBaos.AddRange(danhSachTb);
                await _dbcontex.SaveChangesAsync();

                var soThongBaoMoi = await _dbcontex.ThongBaos
                    .Where(t => danhSachAdminId.Contains(t.UserId) && !t.IsRead)
                    .GroupBy(t => t.UserId)
                    .Select(g => new { UserId = g.Key, SoMoi = g.Count() })
                    .ToDictionaryAsync(x => x.UserId, x => x.SoMoi);

                var tasks = danhSachAdminId.Select(async uid =>
                {
                    var soMoi = soThongBaoMoi.TryGetValue(uid, out var count) ? count : 0;

                    await _hub.Clients
                        .Group($"user_{uid}")
                        .SendAsync("NhanThongBaoMoi", new
                        {
                            type = "report",
                            message = $"Có báo cáo mới: [{targetType}] - \"{reason}\"",
                            link,
                            time = DateTime.Now.ToString("HH:mm dd/MM/yyyy"),
                            soMoi
                        });
                });

                await Task.WhenAll(tasks);
            }

            return Json(new { success = true, message = "Báo cáo đã được gửi, cảm ơn bạn!" });
        }
    }
}
