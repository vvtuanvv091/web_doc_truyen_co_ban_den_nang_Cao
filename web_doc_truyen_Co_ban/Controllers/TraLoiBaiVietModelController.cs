//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.SignalR;
//using Microsoft.EntityFrameworkCore;
//using System.Security.Claims;
//using web_doc_truyen_Co_ban.Data;
//using web_doc_truyen_Co_ban.Hubs;
//using web_doc_truyen_Co_ban.Models;

//namespace web_doc_truyen_Co_ban.Controllers
//{
//    [Authorize]
//    public class TraLoiBaiVietModelController : Controller
//    {
//        private readonly ApplicationDbContext _dbcontext;
//        private const int CooldownSeconds = 60;
//        private const int MaxRepliesPerThread = 20;
//        private readonly IHubContext<NotificationHub> _hub;
//        public TraLoiBaiVietModelController(ApplicationDbContext db, IHubContext<NotificationHub> hub)
//        {
//            _dbcontext = db;
//            _hub = hub;
//        }

//        public IActionResult Index()
//        {
//            return View();
//        }
//        private Guid? GetCurrentUserId()
//        {
//            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
//            return Guid.TryParse(raw, out var id) ? id : null;
//        }
//        // -------------------------------------------------------
//        // POST /TraLoiBaiViet/Create
//        // -------------------------------------------------------
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create(Guid threadId, string content)
//        {
//            var userId = GetCurrentUserId();
//            if (userId == null) return Unauthorized();

//            // ── Kiểm tra bài viết tồn tại & chưa bị khóa ────────
//            var thread = await _dbcontext.Threads.FindAsync(threadId);
//            if (thread == null)
//            {
//                TempData["Error"] = "Bài viết không tồn tại.";
//                return RedirectToAction("Index", "BaiViet");
//            }

//            if (thread.IsLocked)
//            {
//                TempData["Error"] = "Bài viết đã bị khóa, không thể bình luận.";
//                return RedirectToAction("Detail", "BaiViet", new { id = threadId });
//            }

//            // ── Kiểm tra nội dung ─────────────────────────────────
//            content = content?.Trim() ?? "";
//            if (string.IsNullOrEmpty(content))
//            {
//                TempData["Error"] = "Nội dung bình luận không được để trống.";
//                return RedirectToAction("Detail", "BaiViet", new { id = threadId });
//            }

//            if (content.Length > 2000)
//            {
//                TempData["Error"] = "Bình luận không được vượt quá 2000 ký tự.";
//                return RedirectToAction("Detail", "BaiViet", new { id = threadId });
//            }

//            // ── Chống spam: kiểm tra cooldown (60 giây) ──────────
//            var lastReply = await _dbcontext.Replies
//                .Where(r => r.UserId == userId.Value && r.ThreadId == threadId)
//                .OrderByDescending(r => r.CreatedAt)
//                .FirstOrDefaultAsync();

//            if (lastReply != null)
//            {
//                var secondsSinceLast = (DateTime.UtcNow - lastReply.CreatedAt).TotalSeconds;
//                if (secondsSinceLast < CooldownSeconds)
//                {
//                    int remaining = (int)(CooldownSeconds - secondsSinceLast);
//                    TempData["Error"] = $"Bạn cần chờ thêm {remaining} giây trước khi bình luận tiếp.";
//                    return RedirectToAction("Detail", "BaiViet", new { id = threadId });
//                }
//            }

//            // ── Chống spam: giới hạn số reply / bài ──────────────
//            var replyCount = await _dbcontext.Replies
//                .CountAsync(r => r.UserId == userId.Value && r.ThreadId == threadId);

//            if (replyCount >= MaxRepliesPerThread)
//            {
//                TempData["Error"] = $"Bạn đã đạt giới hạn {MaxRepliesPerThread} bình luận trong bài viết này.";
//                return RedirectToAction("Detail", "BaiViet", new { id = threadId });
//            }

//            // ── Lưu reply ─────────────────────────────────────────
//            var reply = new TraLoiBaiVietModel
//            {
//                Id = Guid.NewGuid(),
//                ThreadId = threadId,
//                UserId = userId.Value,
//                Content = content,
//                CreatedAt = DateTime.UtcNow
//            };

//            _dbcontext.Replies.Add(reply);

//            // Cập nhật ReplyCount trên thread
//            thread.ReplyCount++;

//            await _dbcontext.SaveChangesAsync();

//            //// ── Tạo thông báo cho chủ bài (nếu không phải tự reply bài mình) ──
//            //if (thread.UserId != userId.Value)
//            //{
//            //    var notification = new ThongBaoModel
//            //    {
//            //        Id = Guid.NewGuid(),
//            //        UserId = thread.UserId,           // người nhận = chủ bài
//            //        SenderId = userId.Value,            // người gửi = người comment
//            //        Type = "reply",
//            //        Message = $"đã bình luận vào bài viết của bạn: \"{TruncateTitle(thread.Title, 50)}\"",
//            //        Link = $"/BaiViet/Detail/{threadId}#reply-{reply.Id}",
//            //        IsRead = false,
//            //        CreatedAt = DateTime.UtcNow
//            //    };
//            //    _dbcontext.ThongBaos.Add(notification);
//            //    await _dbcontext.SaveChangesAsync();
//            //}

//            TempData["Success"] = "Đã gửi bình luận thành công!";
//            return RedirectToAction("Detail", "BaiViet", new { id = threadId }, $"reply-{reply.Id}");
//        }

//        // -------------------------------------------------------
//        // POST /TraLoiBaiViet/Delete/{id}  — chủ reply hoặc Admin
//        // -------------------------------------------------------
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Delete(Guid id)
//        {
//            var userId = GetCurrentUserId();
//            if (userId == null) return Unauthorized();

//            var reply = await _dbcontext.Replies.FindAsync(id);
//            if (reply == null) return NotFound();

//            if (reply.UserId != userId.Value && !User.IsInRole("Admin"))
//                return Forbid();

//            var threadId = reply.ThreadId;

//            // Giảm ReplyCount
//            var thread = await _dbcontext.Threads.FindAsync(threadId);
//            if (thread != null && thread.ReplyCount > 0)
//                thread.ReplyCount--;

//            _dbcontext.Replies.Remove(reply);
//            await _dbcontext.SaveChangesAsync();

//            TempData["Success"] = "Đã xóa bình luận.";
//            return Redirect($"/BaiViet/Detail/{threadId}#reply-{reply.Id}");
//        }

//        // ── Helper ───────────────────────────────────────────────
//        private static string TruncateTitle(string title, int max) =>
//            title.Length <= max ? title : title[..max] + "...";

//    }
//}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Models;
using web_doc_truyen_Co_ban.Hubs;

namespace web_doc_truyen_Co_ban.Controllers
{
    [Authorize]
    public class TraLoiBaiVietModelController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;
        private readonly IHubContext<NotificationHub> _hub;   // ← thêm
        private const int CooldownSeconds = 60;
        private const int MaxRepliesPerThread = 20;

        public TraLoiBaiVietModelController(
            ApplicationDbContext db,
            IHubContext<NotificationHub> hub)   // ← inject
        {
            _dbcontext = db;
            _hub = hub;
        }

        public IActionResult Index() => View();

        private Guid? GetCurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : null;
        }

        // -------------------------------------------------------
        // POST /TraLoiBaiViet/Create
        // -------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Guid threadId, string content)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            // ── Kiểm tra bài viết ────────────────────────────────
            var thread = await _dbcontext.Threads.FindAsync(threadId);
            if (thread == null)
            {
                TempData["Error"] = "Bài viết không tồn tại.";
                return RedirectToAction("Index", "BaiViet");
            }

            if (thread.IsLocked)
            {
                TempData["Error"] = "Bài viết đã bị khóa, không thể bình luận.";
                return RedirectToAction("Detail", "BaiViet", new { id = threadId });
            }

            // ── Kiểm tra nội dung ─────────────────────────────────
            content = content?.Trim() ?? "";
            if (string.IsNullOrEmpty(content))
            {
                TempData["Error"] = "Nội dung bình luận không được để trống.";
                return RedirectToAction("Detail", "BaiViet", new { id = threadId });
            }

            if (content.Length > 2000)
            {
                TempData["Error"] = "Bình luận không được vượt quá 2000 ký tự.";
                return RedirectToAction("Detail", "BaiViet", new { id = threadId });
            }

            // ── Cooldown 60 giây ──────────────────────────────────
            var lastReply = await _dbcontext.Replies
                .Where(r => r.UserId == userId.Value && r.ThreadId == threadId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastReply != null)
            {
                var secondsSinceLast = (DateTime.UtcNow - lastReply.CreatedAt).TotalSeconds;
                if (secondsSinceLast < CooldownSeconds)
                {
                    int remaining = (int)(CooldownSeconds - secondsSinceLast);
                    TempData["Error"] = $"Bạn cần chờ thêm {remaining} giây trước khi bình luận tiếp.";
                    return RedirectToAction("Detail", "BaiViet", new { id = threadId });
                }
            }

            // ── Giới hạn số reply ─────────────────────────────────
            var replyCount = await _dbcontext.Replies
                .CountAsync(r => r.UserId == userId.Value && r.ThreadId == threadId);

            if (replyCount >= MaxRepliesPerThread)
            {
                TempData["Error"] = $"Bạn đã đạt giới hạn {MaxRepliesPerThread} bình luận trong bài viết này.";
                return RedirectToAction("Detail", "BaiViet", new { id = threadId });
            }

            // ── Lưu reply ─────────────────────────────────────────
            var reply = new TraLoiBaiVietModel
            {
                Id = Guid.NewGuid(),
                ThreadId = threadId,
                UserId = userId.Value,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _dbcontext.Replies.Add(reply);
            thread.ReplyCount++;

            // ── Tạo thông báo (chỉ khi không tự reply bài mình) ──
            if (thread.UserId != userId.Value)
            {
                // Lấy tên người đang comment
                var sender = await _dbcontext.Users.FindAsync(userId.Value);
                var tenSender = sender?.Username ?? "Ai đó";

                var notification = new ThongBaoModel
                {
                    UserId = thread.UserId,
                    SenderId = userId.Value,
                    Type = "reply",
                    Message = $"{tenSender} đã bình luận vào bài viết của bạn: \"{TruncateTitle(thread.Title, 50)}\"",
                    Link = $"/BaiViet/Detail/{threadId}#reply-{reply.Id}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _dbcontext.ThongBaos.Add(notification);
                await _dbcontext.SaveChangesAsync();   // save để có Id notification

                // Đếm tổng chưa đọc của chủ bài
                var soMoi = await _dbcontext.ThongBaos
                    .CountAsync(t => t.UserId == thread.UserId && !t.IsRead);

                // Push realtime đến đúng chủ bài
                await _hub.Clients
                    .Group($"user_{thread.UserId}")
                    .SendAsync("NhanThongBaoMoi", new
                    {
                        id = notification.Id,
                        type = notification.Type,
                        message = notification.Message,
                        link = notification.Link,
                        time = notification.CreatedAt.ToString("HH:mm dd/MM/yyyy"),
                        soMoi
                    });
            }
            else
            {
                await _dbcontext.SaveChangesAsync();   // save nếu không có notification
            }

            TempData["Success"] = "Đã gửi bình luận thành công!";
            return RedirectToAction("Detail", "BaiViet", new { id = threadId }, $"reply-{reply.Id}");
        }

        // -------------------------------------------------------
        // POST /TraLoiBaiViet/Delete/{id}
        // -------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var reply = await _dbcontext.Replies.FindAsync(id);
            if (reply == null) return NotFound();

            if (reply.UserId != userId.Value && !User.IsInRole("Admin"))
                return Forbid();

            var threadId = reply.ThreadId;

            var thread = await _dbcontext.Threads.FindAsync(threadId);
            if (thread != null && thread.ReplyCount > 0)
                thread.ReplyCount--;

            _dbcontext.Replies.Remove(reply);
            await _dbcontext.SaveChangesAsync();

            TempData["Success"] = "Đã xóa bình luận.";
            return Redirect($"/BaiViet/Detail/{threadId}#reply-{reply.Id}");
        }

        // ── Helper ───────────────────────────────────────────────
        private static string TruncateTitle(string title, int max) =>
            title.Length <= max ? title : title[..max] + "...";
    }
}