using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Net.NetworkInformation;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Hubs;
using web_doc_truyen_Co_ban.Models;

namespace web_doc_truyen_Co_ban.Controllers
{


    public class ChapterController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;
        private readonly IHubContext<NotificationHub> _hub;
        public ChapterController(ApplicationDbContext context, IHubContext<NotificationHub> hub)
        {
            _dbcontext = context;
            _hub = hub;
        }
        public async Task<IActionResult> Index(Guid? id = null)
        {
            if (id == null)
                return RedirectToAction("Index", "Home");

            var chapter_read = await _dbcontext.Chapters
                .Include(x => x.ChapterViews)
                .Include(x => x.ChapterComments)
                .Include(x => x.Story)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (chapter_read == null)
                return NotFound();


            // ─────────────────────────────────────
            // Danh sách chapter
            // ─────────────────────────────────────
            var allchapter = await _dbcontext.Chapters
                .Where(x => x.StoryId == chapter_read.StoryId)
                .OrderBy(c => c.ChapterNumber)
                .Select(c => new
                {
                    c.Id,
                    c.ChapterNumber,
                    c.Title
                })
                .ToListAsync();

            ViewBag.AllChapters = allchapter;
            ViewBag.CurrentChapter = id;

            // ─────────────────────────────────────
            // Prev / Next chapter
            // ─────────────────────────────────────
            ViewBag.PrevChapter_Id = await _dbcontext.Chapters
                .Where(x =>
                    x.StoryId == chapter_read.StoryId &&
                    x.ChapterNumber < chapter_read.ChapterNumber)
                .OrderByDescending(x => x.ChapterNumber)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync();

            ViewBag.NextChapter_Id = await _dbcontext.Chapters
                .Where(x =>
                    x.StoryId == chapter_read.StoryId &&
                    x.ChapterNumber > chapter_read.ChapterNumber)
                .OrderBy(x => x.ChapterNumber)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync();

            // ─────────────────────────────────────
            // User hiện tại
            // ─────────────────────────────────────
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (chapter_read.IsLocked || chapter_read.Status == 2)
            {
                if (string.IsNullOrEmpty(userIdStr))
                {
                    TempData["Error"] = "Vui lòng đăng nhập để đọc chương VIP";
                    return RedirectToAction("Login", "Account");
                }

                var userId = Guid.Parse(userIdStr);

                var unlocked = await _dbcontext.UnlockedChapters
                    .AnyAsync(x => x.UserId == userId && x.ChapterId == chapter_read.Id);

                if (!unlocked)
                    return RedirectToAction("Confirm", "UnlockChapter", new { id = chapter_read.Id });
            }

            // ─────────────────────────────────────
            // Chống spam view bằng session
            // ─────────────────────────────────────
            var sessionKey = $"viewed_chapter_{chapter_read.Id}";

            if (HttpContext.Session.GetString(sessionKey) == null)
            {
                // tăng view chapter
                chapter_read.ViewCount++;

                // tăng view story
                if (chapter_read.Story != null)
                {
                    chapter_read.Story.TotalViews++;
                    chapter_read.Story.UpdatedAt = DateTime.UtcNow;
                }

                // lưu chapter view
                var chapterView = new Models.ChapterViewModel
                {
                    Id = Guid.NewGuid(),
                    ChapterId = chapter_read.Id,
                    StoryId = chapter_read.StoryId,
                    ViewedAt = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                };

                // nếu login thì lưu user
                if (!string.IsNullOrEmpty(userIdStr))
                {
                    chapterView.UserId = Guid.Parse(userIdStr);
                }

                _dbcontext.ChapterViews.Add(chapterView);

                // chống spam F5
                HttpContext.Session.SetString(sessionKey, "1");
            }

            // ─────────────────────────────────────
            // Reading History
            // ─────────────────────────────────────
            if (!string.IsNullOrEmpty(userIdStr))
            {
                var userId = Guid.Parse(userIdStr);

                var history = await _dbcontext.ReadingHistories
                    .FirstOrDefaultAsync(h =>
                        h.UserId == userId &&
                        h.StoryId == chapter_read.StoryId);

                if (history == null)
                {
                    _dbcontext.ReadingHistories.Add(new Models.ReadingHistoryModel
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        StoryId = chapter_read.StoryId,
                        ChapterId = chapter_read.Id,
                        ChapterNumber = chapter_read.ChapterNumber,
                        LastReadAt = DateTime.UtcNow
                    });
                }
                else
                {
                    history.ChapterId = chapter_read.Id;
                    history.ChapterNumber = chapter_read.ChapterNumber;
                    history.LastReadAt = DateTime.UtcNow;
                }
            }

            await _dbcontext.SaveChangesAsync();

            return View(chapter_read);
        }


        [HttpGet]
        public async Task<IActionResult> GetComments(Guid chapterId, int page = 1, int pageSize = 5)
        {
            var comments = await _dbcontext.ChapterComments
                .Where(c => c.ChapterId == chapterId && c.ParentId == null && c.Status == 0)
                .Include(c => c.User)
                .Include(c => c.Replies.Where(r => r.Status == 0))
                    .ThenInclude(r => r.User)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new {
                    c.Id,
                    c.Content,
                    c.LikeCount,
                    c.CreatedAt,
                    UserName = c.User.Username,
                    Avatar = c.User.AvatarUrl,
                    Replies = c.Replies.Select(r => new {
                        r.Id,
                        r.Content,
                        r.LikeCount,
                        r.CreatedAt,
                        UserName = r.User.Username,
                        Avatar = r.User.AvatarUrl,
                    })
                })
                .ToListAsync();

            var total = await _dbcontext.ChapterComments
                .CountAsync(c => c.ChapterId == chapterId && c.ParentId == null && c.Status == 0);

            return Json(new { comments, total, page, pageSize });
        }
        [HttpPost]
        public async Task<IActionResult> PostComment([FromBody] PostCommentDto dto)
        {
            // Chưa đăng nhập
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr))
                return Unauthorized(new { message = "Vui lòng đăng nhập để bình luận" });

            // Validate model
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Nội dung bình luận không hợp lệ" });

            // Không tìm thấy chapter
            var chapter = await _dbcontext.Chapters.FindAsync(dto.ChapterId);
            if (chapter == null)
                return NotFound(new { message = "Không tìm thấy chương" });

            var comment = new ChapterCommentModel
            {
                Id = Guid.NewGuid(),
                ChapterId = dto.ChapterId,
                StoryId = chapter.StoryId,
                UserId = Guid.Parse(userIdStr),
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbcontext.ChapterComments.Add(comment);
            await _dbcontext.SaveChangesAsync();

            return Ok(new
            {
                id = comment.Id,
                content = comment.Content,
                createdAt = comment.CreatedAt,
                userName = HttpContext.Session.GetString("Username"),
                avatar = HttpContext.Session.GetString("AvatarUrl")
            });
        }
        [HttpPost]
        public async Task<IActionResult> LikeComment(Guid id)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            var senderId = Guid.Parse(userIdStr);

            var comment = await _dbcontext.ChapterComments.FindAsync(id);
            if (comment == null || comment.Status != 0) return NotFound();

            var sessionKey = $"liked_comment_{id}";
            var liked = HttpContext.Session.GetString(sessionKey);

            if (liked == null)
            {
                comment.LikeCount++;
                HttpContext.Session.SetString(sessionKey, "1");

                // Gửi thông báo nếu like người khác
                if (comment.UserId != senderId)
                {
                    var sender = await _dbcontext.Users
                        .Where(u => u.Id == senderId)
                        .Select(u => new { u.Username })
                        .FirstOrDefaultAsync();

                    var tb = new ThongBaoModel
                    {
                        UserId = comment.UserId,
                        SenderId = senderId,
                        Type = "like",
                        Message = $"{sender?.Username ?? "Ai đó"} đã thích bình luận của bạn.",
                        Link = $"/Chapter/Index?id={comment.ChapterId}#cmt-{comment.Id}",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _dbcontext.ThongBaos.Add(tb);
                    await _dbcontext.SaveChangesAsync();

                    // Push realtime
                    var soMoi = await _dbcontext.ThongBaos
                        .CountAsync(t => t.UserId == comment.UserId && !t.IsRead);

                    await _hub.Clients
                        .Group($"user_{comment.UserId}")
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
            }
            else
            {
                comment.LikeCount = Math.Max(0, comment.LikeCount - 1);
                HttpContext.Session.Remove(sessionKey);
            }

            await _dbcontext.SaveChangesAsync();
            return Ok(new { likeCount = comment.LikeCount, liked = liked == null });
        }
        public class PostCommentDto
        {
            [Required]
            public Guid ChapterId { get; set; }
            public string Content { get; set; }
        }
    }
}
