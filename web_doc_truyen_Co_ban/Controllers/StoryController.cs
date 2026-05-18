using LinqToDB;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Models;

namespace web_doc_truyen_Co_ban.Controllers
{
    public class StoryController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;
 

        public StoryController(ApplicationDbContext context)
        {
            _dbcontext = context;
            
        }
        public IActionResult Index()
        {
            return View();
        }
        public async Task <IActionResult> Detail(Guid? id =null)
        {
            if(id==null)
            {
                return RedirectToAction("Index");

            }
            var stories = _dbcontext.Stories
                .Include(x=>x.Category)
                .Include(x=>x.Tags)
                .Include(x=>x.Chapters)
                .Include (x=>x.Ratings)
                .Include(x=>x.StoryComments)
                .ThenInclude(x=>x.User)

                .Where(x => x.Id == id).FirstOrDefault();
            if(stories==null)
            {
                return NotFound();
            }
            // ── Lấy lịch sử đọc ──
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdStr))
            {
                var userId = Guid.Parse(userIdStr);
                var history = await _dbcontext.ReadingHistories
                    .FirstOrDefaultAsync(h => h.UserId == userId && h.StoryId == id);

                ViewBag.ContinueChapterId = history?.ChapterId;
                ViewBag.ContinueChapterNumber = history?.ChapterNumber;
            }

            // Chương đầu tiên để đọc từ đầu
            var firstChapter = stories.Chapters
                .Where(c=>c.Status==1|| c.Status==2)
                .OrderBy(c => c.ChapterNumber)
                .FirstOrDefault();
            ViewBag.FirstChapterId = firstChapter?.Id;
            return View(stories);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(Guid storyId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return Redirect(Url.Action("Detail", new { id = storyId }) + "#binhluan");

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var comment = new StoryCommentModel
            {
                Id = Guid.NewGuid(),
                StoryId = storyId,
                UserId = Guid.Parse(userId),
                Content = content.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbcontext.StoryComments.Add(comment);
            await _dbcontext.SaveChangesAsync();

            // Redirect về đúng tab bình luận
            return Redirect(Url.Action("Detail", new { id = storyId }) + "#binhluan");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LikeComment(Guid commentId, Guid storyId)
        {
            var comment = await _dbcontext.StoryComments.FindAsync(commentId);
            if (comment != null)
            {
                comment.LikeCount++;
                await _dbcontext.SaveChangesAsync();
            }
            return Redirect(Url.Action("Detail", new { id = storyId }) + "#binhluan");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rate(Guid storyId, byte score)
        {
            if (score < 1 || score > 5)
                return Json(new { success = false, message = "Điểm không hợp lệ." });

            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr))
                return Json(new { success = false, message = "Bạn cần đăng nhập." });

            var userId = Guid.Parse(userIdStr);
            var story = await _dbcontext.Stories.FindAsync(storyId);
            if (story == null) return Json(new { success = false, message = "Không tìm thấy truyện." });

            var existing = await _dbcontext.Ratings
                .FirstOrDefaultAsync(r => r.UserId == userId && r.StoryId == storyId);

            if (existing == null)
            {
                _dbcontext.Ratings.Add(new RatingModel
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    StoryId = storyId,
                    Score = score,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                story.RatingSum += score;
                story.RatingCount += 1;
            }
            else
            {
                story.RatingSum -= existing.Score;
                story.RatingSum += score;
                existing.Score = score;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            story.RatingAvg = story.RatingCount > 0
                ? Math.Round((decimal)story.RatingSum / story.RatingCount, 2) : 0;
            story.UpdatedAt = DateTime.UtcNow;

            await _dbcontext.SaveChangesAsync();

            return Json(new
            {
                success = true,
                avgRating = story.RatingAvg,
                ratingCount = story.RatingCount,
                userScore = score
            });
        }
        public async Task<IActionResult> StatusStory(byte? status,int page=1,int pageSize=20)
        {
            var querystatus = _dbcontext.Stories
                .Include(s => s.Category)
                .Include(s => s.Tags)
                .AsQueryable(); 
            if (status.HasValue)
            {
                querystatus=querystatus.Where(x => x.Status == status.Value);
            }    
            querystatus=querystatus.OrderByDescending(x => x.UpdatedAt);
            var totalItems = await querystatus.CountAsync();
            var totalPages= await querystatus.Skip((page-1)*pageSize).Take(pageSize).ToListAsync();

            //bắt đầu bắt ddeems trang thai
            // Status: 0=Bản nháp, 1=Đang ra, 2=Hoàn thành, 3=Tạm dừng, 4=Drop
            ViewBag.Count_All = await _dbcontext.Stories.CountAsync();
            ViewBag.Count_Draft = await _dbcontext.Stories.CountAsync(x => x.Status == 0);
            ViewBag.Count_Ongoing = await _dbcontext.Stories.CountAsync(x => x.Status == 1);
            ViewBag.Count_Done = await _dbcontext.Stories.CountAsync(x => x.Status == 2);
            ViewBag.Count_Pause = await _dbcontext.Stories.CountAsync(x => x.Status == 3);
            ViewBag.Count_Drop = await _dbcontext.Stories.CountAsync(x => x.Status == 4);

            ViewBag.CurrentStatus = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.TotalItems = totalItems;

            return View(totalPages);
        }
    }
}
