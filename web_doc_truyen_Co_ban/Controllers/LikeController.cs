using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Models;

namespace web_doc_truyen_Co_ban.Controllers
{
    [Authorize]
    public class LikeController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;
        public LikeController(ApplicationDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }
        private Guid? GetCurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : null;
        }

        // -------------------------------------------------------
        // POST /Like/Toggle/{replyId}
        // -------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(Guid replyId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            // Lấy reply + thread để redirect về đúng bài
            var reply = await _dbcontext.Replies
                .FirstOrDefaultAsync(r => r.Id == replyId);

            if (reply == null) return NotFound();

            // Kiểm tra đã like chưa — giống Follow
            var existing = await _dbcontext.Likes
                .FirstOrDefaultAsync(l => l.ReplyId == replyId && l.UserId == userId.Value);

            if (existing != null)
            {
                // Bỏ like
                _dbcontext.Likes.Remove(existing);
                if (reply.LikeCount > 0) reply.LikeCount--;
            }
            else
            {
                // Thêm like
                _dbcontext.Likes.Add(new LikeModel
                {
                    ReplyId = replyId,
                    UserId = userId.Value
                });
                reply.LikeCount++;

                // Tạo thông báo cho chủ reply (nếu không phải tự like)
                if (reply.UserId != userId.Value)
                {
                    var sender = await _dbcontext.Users
                        .FirstOrDefaultAsync(u => u.Id == userId.Value);

                    var thongBao = new ThongBaoModel
                    {
                        UserId = reply.UserId,
                        SenderId = userId.Value,
                        Type = "like",
                        Message = $"{sender?.Username ?? "Ai đó"} đã thích bình luận của bạn",
                        Link = $"/BaiViet/Detail/{reply.ThreadId}#reply-{reply.Id}",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _dbcontext.ThongBaos.Add(thongBao);
                }
            }

            await _dbcontext.SaveChangesAsync();
            // Redirect về đúng bài viết, scroll đến reply
            return Redirect($"/BaiViet/Detail/{reply.ThreadId}#reply-{reply.Id}");
        }
    }
}
