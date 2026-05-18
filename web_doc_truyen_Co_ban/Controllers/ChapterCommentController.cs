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
        public ChapterCommentController(ApplicationDbContext db) => _dbcontext = db;

        private Guid? CurrentUserId()
        {
            var val = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(val, out var id) ? id : null;
        }

        // =====================================================================
        // POST /ChapterComment/Add
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
            if (parentId.HasValue)
            {
                var parent = await _dbcontext.ChapterComments
                    .AsNoTracking()
                    .Where(c => c.Id == parentId.Value && c.ChapterId == chapterId && c.Status != 2)
                    .Select(c => new { c.Id, c.ParentId })
                    .FirstOrDefaultAsync();

                if (parent == null)
                {
                    TempData["CommentError"] = "Bình luận gốc không tồn tại.";
                    return RedirectToAction("Index", "Chapter", new { id = chapterId });
                }

                rootParentId = parent.ParentId ?? parent.Id;
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
            // Gửi thông báo reply
            if (rootParentId.HasValue)
            {
                var parentComment = await _dbcontext.ChapterComments
                    .Where(c => c.Id == rootParentId.Value)
                    .Select(c => new { c.UserId })
                    .FirstOrDefaultAsync();

                if (parentComment != null && parentComment.UserId != userId.Value)
                {
                    var sender = await _dbcontext.Users
                        .Where(u => u.Id == userId.Value)
                        .Select(u => new { u.Username })
                        .FirstOrDefaultAsync();

                    var thongBaoCtrl = new ThongBaoController(
                        HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>(),
                        HttpContext.RequestServices.GetRequiredService<IHubContext<NotificationHub>>()
                    );
                    thongBaoCtrl.ControllerContext = ControllerContext;

                    await thongBaoCtrl.PushThongBaoReply(
                        parentComment.UserId,
                        userId.Value,
                        sender?.Username ?? "Ai đó",
                        $"/Chapter/Index?id={chapterId}#cmt-{comment.Id}"
                    );
                }
            }

            return RedirectToAction("Index", "Chapter", new { id = chapterId });
        }

        // =====================================================================
        // POST /ChapterComment/Edit
        // =====================================================================
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

            return RedirectToAction("Index", "Chapter", new { id = chapterId });
        }

        // =====================================================================
        // POST /ChapterComment/Delete
        // =====================================================================
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
            return RedirectToAction("Index", "Chapter", new { id = chapterId });
        }

        // =====================================================================
        // POST /ChapterComment/Like
        // =====================================================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Like(Guid id, Guid chapterId, bool isLike)
        {
            var comment = await _dbcontext.ChapterComments.FindAsync(id);
            if (comment == null || comment.Status != 0) return NotFound();

            comment.LikeCount = isLike
                ? comment.LikeCount + 1
                : Math.Max(0, comment.LikeCount - 1);
            comment.UpdatedAt = DateTime.UtcNow;

            await _dbcontext.SaveChangesAsync();
            return RedirectToAction("Index", "Chapter", new { id = chapterId });
        }
    }
}