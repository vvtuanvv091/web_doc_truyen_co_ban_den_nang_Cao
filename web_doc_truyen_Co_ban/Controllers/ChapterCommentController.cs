using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Hubs;
using web_doc_truyen_Co_ban.Models;

namespace web_doc_truyen_Co_ban.Controllers
{
    public class ChapterCommentController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;
        private readonly IHubContext<NotificationHub> _hub;
        private readonly ThongBaoController _thongbao;
        public ChapterCommentController(ApplicationDbContext db, IHubContext<NotificationHub> hub)
        {
            _dbcontext = db;
            _hub = hub;
        }

        private Guid? CurrentUserId()
        {
            var val = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(val, out var id) ? id : null;
        }

        // POST /ChapterComment/Add
        //[HttpPost]
        //[Authorize]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Add(Guid chapterId, string content, Guid? parentId)
        //{
        //    var userId = CurrentUserId();
        //    if (userId == null) return Unauthorized();

        //    if (string.IsNullOrWhiteSpace(content) || content.Length > 2000)
        //    {
        //        TempData["CommentError"] = "Nội dung bình luận không hợp lệ (1-2000 ký tự).";
        //        return RedirectToAction("Index", "Chapter", new { id = chapterId });
        //    }

        //    var chapter = await _dbcontext.Chapters
        //        .AsNoTracking()
        //        .Where(c => c.Id == chapterId)
        //        .Select(c => new { c.Id, c.StoryId })
        //        .FirstOrDefaultAsync();

        //    if (chapter == null) return NotFound();

        //    Guid? rootParentId = null;
        //    if (parentId.HasValue)
        //    {
        //        var parent = await _dbcontext.ChapterComments
        //            .AsNoTracking()
        //            .Where(c => c.Id == parentId.Value && c.ChapterId == chapterId && c.Status != 2)
        //            .Select(c => new { c.Id, c.ParentId })
        //            .FirstOrDefaultAsync();

        //        if (parent == null)
        //        {
        //            TempData["CommentError"] = "Bình luận gốc không tồn tại.";
        //            return RedirectToAction("Index", "Chapter", new { id = chapterId });
        //        }

        //        rootParentId = parent.ParentId ?? parent.Id;
        //    }

        //    var comment = new ChapterCommentModel
        //    {
        //        Id = Guid.NewGuid(),
        //        ChapterId = chapterId,
        //        StoryId = chapter.StoryId,
        //        UserId = userId.Value,
        //        ParentId = rootParentId,
        //        Content = content.Trim(),
        //        Status = 0,
        //        CreatedAt = DateTime.UtcNow,
        //        UpdatedAt = DateTime.UtcNow
        //    };

        //    _dbcontext.ChapterComments.Add(comment);
        //    await _dbcontext.SaveChangesAsync();
        //    // Gửi thông báo reply
        //    if (rootParentId.HasValue)
        //    {
        //        var parentComment = await _dbcontext.ChapterComments
        //            .Where(c => c.Id == rootParentId.Value)
        //            .Select(c => new { c.UserId })
        //            .FirstOrDefaultAsync();

        //        if (parentComment != null && parentComment.UserId != userId.Value)
        //        {
        //            var sender = await _dbcontext.Users
        //                .Where(u => u.Id == userId.Value)
        //                .Select(u => new { u.Username })
        //                .FirstOrDefaultAsync();

        //            var thongBaoCtrl = new ThongBaoController(
        //                HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>(),
        //                HttpContext.RequestServices.GetRequiredService<IHubContext<NotificationHub>>()
        //            );
        //            thongBaoCtrl.ControllerContext = ControllerContext;

        //            await thongBaoCtrl.PushThongBaoReply(
        //                parentComment.UserId,
        //                userId.Value,
        //                sender?.Username ?? "Ai đó",
        //                $"/Chapter/Index?id={chapterId}#cmt-{comment.Id}"
        //            );
        //        }
        //    }

        //    return RedirectToAction("Index", "Chapter", new { id = chapterId });
        //}

        // =====================================================================
        // POST /ChapterComment/Edit
        // =====================================================================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Guid chapterId, string content, Guid? parentId)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(content) || content.Length > 2000)
            {
                TempData["CommentError"] = "Nội dung bình luận không hợp lệ (1-2000 ký tự).";
                return RedirectToAction("Index", "Chapter", new { id = chapterId });
            }

            var chapter = await _dbcontext.Chapters
                .AsNoTracking()
                .Where(c => c.Id == chapterId)
                .Select(c => new { c.Id, c.StoryId })
                .FirstOrDefaultAsync();

            if (chapter == null) return NotFound();

            Guid? rootParentId = null;
            Guid? directParentUserId = null; // ← người được reply trực tiếp

            if (parentId.HasValue)
            {
                var parent = await _dbcontext.ChapterComments
                    .AsNoTracking()
                    .Where(c => c.Id == parentId.Value && c.ChapterId == chapterId && c.Status != 2)
                    .Select(c => new { c.Id, c.ParentId, c.UserId })
                    .FirstOrDefaultAsync();

                if (parent == null)
                {
                    TempData["CommentError"] = "Bình luận gốc không tồn tại.";
                    return RedirectToAction("Index", "Chapter", new { id = chapterId });
                }

                rootParentId = parent.ParentId ?? parent.Id;
                directParentUserId = parent.UserId; // ← lấy userId của người bị reply
            }

            var comment = new ChapterCommentModel
            {
                Id = Guid.NewGuid(),
                ChapterId = chapterId,
                StoryId = chapter.StoryId,
                UserId = userId.Value,
                ParentId = rootParentId,
                Content = content.Trim(),
                Status = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbcontext.ChapterComments.Add(comment);
            await _dbcontext.SaveChangesAsync();

            //// ── Gửi thông báo reply ──────────────────────────────────
            //if (directParentUserId.HasValue && directParentUserId.Value != userId.Value)
            //{
            //    var sender = await _dbcontext.Users
            //        .Where(u => u.Id == userId.Value)
            //        .Select(u => new { u.Username })
            //        .FirstOrDefaultAsync();

            //    await _thongbao.PushThongBaoReply(
            //        userNhanId: directParentUserId.Value,           // ← người được reply
            //        senderId: userId.Value,
            //        tenSender: sender?.Username ?? "Ai đó",
            //        linkComment: $"/Chapter/Index?id={chapterId}#cmt-{comment.Id}"
            //    );
            //}
            // ── Gửi thông báo reply ──────────────────────────────────
            if (directParentUserId.HasValue && directParentUserId.Value != userId.Value)
            {
                var sender = await _dbcontext.Users
                    .Where(u => u.Id == userId.Value)
                    .Select(u => new { u.Username })
                    .FirstOrDefaultAsync();

                // Khởi tạo trực tiếp ThongBaoController bằng ServiceProvider của Request hiện tại
                var thongBaoCtrl = new ThongBaoController(
                    HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>(),
                    HttpContext.RequestServices.GetRequiredService<IHubContext<NotificationHub>>()
                );
                thongBaoCtrl.ControllerContext = ControllerContext;

                await thongBaoCtrl.PushThongBaoReply(
                    userNhanId: directParentUserId.Value,
                    senderId: userId.Value,
                    tenSender: sender?.Username ?? "Ai đó",
                    linkComment: $"/Chapter/Index?id={chapterId}#cmt-{comment.Id}"
                );
            }

            return Ok();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Guid chapterId, string content)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(content) || content.Length > 2000)
            {
                TempData["CommentError"] = "Nội dung không hợp lệ.";
                return RedirectToAction("Index", "Chapter", new { id = chapterId });
            }

            var comment = await _dbcontext.ChapterComments.FindAsync(id);
            if (comment == null || comment.Status == 2) return NotFound();
            if (comment.UserId != userId.Value) return Forbid();

            comment.Content = content.Trim();
            comment.UpdatedAt = DateTime.UtcNow;
            await _dbcontext.SaveChangesAsync();

            return Ok();
        }

        // POST /ChapterComment/Delete
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, Guid chapterId)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized();

            var comment = await _dbcontext.ChapterComments.FindAsync(id);
            if (comment == null || comment.Status == 2) return NotFound();
            if (comment.UserId != userId.Value) return Forbid();

            comment.Status = 2;
            comment.UpdatedAt = DateTime.UtcNow;

            var replies = await _dbcontext.ChapterComments
                .Where(c => c.ParentId == id)
                .ToListAsync();
            foreach (var r in replies) { r.Status = 2; r.UpdatedAt = DateTime.UtcNow; }

            await _dbcontext.SaveChangesAsync();
            return Ok();
        }
        // POST /ChapterComment/Like
        //[HttpPost]
        //[Authorize]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Like(Guid id, Guid chapterId, bool isLike)
        //{
        //    var comment = await _dbcontext.ChapterComments.FindAsync(id);
        //    if (comment == null || comment.Status != 0) return NotFound();

        //    comment.LikeCount = isLike
        //        ? comment.LikeCount + 1
        //        : Math.Max(0, comment.LikeCount - 1);
        //    comment.UpdatedAt = DateTime.UtcNow;

        //    await _dbcontext.SaveChangesAsync();
        //    return RedirectToAction("Index", "Chapter", new { id = chapterId });
        //}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Like(Guid commentId, Guid chapterId)
        {
            var comment = await _dbcontext.ChapterComments.FindAsync(commentId);
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var likerId = string.IsNullOrEmpty(userIdStr) ? (Guid?)null : Guid.Parse(userIdStr);

            var chapter = await _dbcontext.Chapters
                .FirstOrDefaultAsync(c => c.Id == chapterId);

            var liker = await _dbcontext.Users
                .FirstOrDefaultAsync(u => u.Id == likerId);

            if (comment != null)
            {
                comment.LikeCount++;
                await _dbcontext.SaveChangesAsync();

                // Không tự thông báo cho chính mình
                if (likerId.HasValue && comment.UserId != likerId.Value)
                {
                    var link = Url.Action("Index", "Chapter", new { id = chapterId }) + $"#cmt-{commentId}";

                    var tb = new ThongBaoModel
                    {
                        UserId = comment.UserId,
                        Type = "like",
                        Message = $"{liker?.DisplayName ?? "Ai đó"} vừa thích bình luận của bạn trong chương \"{chapter?.Title ?? ""}\".",
                        Link = link
                    };
                    _dbcontext.ThongBaos.Add(tb);
                    await _dbcontext.SaveChangesAsync();

                    var soMoi = await _dbcontext.ThongBaos
                        .CountAsync(t => t.UserId == comment.UserId && !t.IsRead);

                    await _hub.Clients
                        .Group($"user_{comment.UserId}")
                        .SendAsync("NhanThongBaoMoi", new
                        {
                            Type = "like",
                            Message = $"{liker?.DisplayName ?? "Ai đó"} vừa thích bình luận của bạn trong chương \"{chapter?.Title ?? ""}\".",
                            link,
                            time = DateTime.Now.ToString("HH:mm dd/MM/yyyy"),
                            soMoi
                        });
                }
            }

            //return RedirectToAction("Index", "Chapter", new { id = chapterId });
            return Ok();
        }
        // GET /ChapterComment/GetList
        [HttpGet]
        public async Task<IActionResult> GetList(Guid chapterId, int page = 1, int pageSize = 10)
        {
            var total = await _dbcontext.ChapterComments
                .CountAsync(c => c.ChapterId == chapterId && c.ParentId == null && c.Status == 0);

            var comments = await _dbcontext.ChapterComments
                .Where(c => c.ChapterId == chapterId && c.ParentId == null && c.Status == 0)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Include(c => c.User)
                .Include(c => c.Replies.Where(r => r.Status == 0))
                    .ThenInclude(r => r.User)
                .ToListAsync();

            var result = comments.Select(c => new {
                id = c.Id,
                userId = c.UserId,
                displayName = c.User?.DisplayName ?? "Ẩn danh",
                avatarUrl = c.User?.AvatarUrl,
                content = c.Content,
                likeCount = c.LikeCount,
                createdAt = c.CreatedAt.ToString("HH:mm · dd/MM/yyyy"),
                replies = c.Replies.OrderBy(r => r.CreatedAt).Select(r => new {
                    id = r.Id,
                    userId = r.UserId,
                    displayName = r.User?.DisplayName ?? "Ẩn danh",
                    avatarUrl = r.User?.AvatarUrl,
                    content = r.Content,
                    likeCount = r.LikeCount,
                    createdAt = r.CreatedAt.ToString("HH:mm · dd/MM/yyyy")
                })
            });

            return Json(new { total, comments = result });
        }

        // GET /ChapterComment/GetLikeCount
        [HttpGet]
        public async Task<IActionResult> GetLikeCount(Guid commentId)
        {
            var count = await _dbcontext.ChapterComments
                .Where(c => c.Id == commentId)
                .Select(c => c.LikeCount)
                .FirstOrDefaultAsync();

            return Json(new { count });
        }
    }
}