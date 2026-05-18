using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using web_doc_truyen_Co_ban.Data;

namespace web_doc_truyen_Co_ban.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BookMarkController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;
        public BookMarkController(ApplicationDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }

        public IActionResult Index(string searach, int page, string storyid,string chapterid)
        {
            ViewData["ActiveMenu"] = "BookMark";
            page = 1;
            var query = _dbcontext.Bookmarks
                .Include(c => c.User)
                .Include(c => c.Story).Include(c => c.Chapter)
                .AsQueryable();
            if(!string.IsNullOrWhiteSpace(searach))
            {
                searach=searach.Trim().ToLower();
                query = query.Where(c => c.User.Username.ToLower().Contains(searach));
                
            }    
            if(!string.IsNullOrWhiteSpace(storyid)&&Guid.TryParse(storyid,out var sid))
            {
                query=query.Where(c=>c.StoryId==sid);
                ViewData["StoryId"] = storyid;
            }
            if (!string.IsNullOrWhiteSpace(chapterid) && Guid.TryParse(chapterid, out var cid))
            {
                query = query.Where(c => c.ChapterId == cid);
                ViewData["ChapterId"] = chapterid;
            }
            ViewBag.ToTalReading = _dbcontext.Bookmarks.Count();
            ViewBag.ToTalUser= _dbcontext.Bookmarks
                .Select(c => c.UserId)
                .Distinct()
                .Count();
            ViewBag.ToTalRead = _dbcontext.Bookmarks
                .Count(c => c.UpdatedAt.Date == DateTime.UtcNow.Date);
            ViewBag.ingAvatarUrl= _dbcontext.Users
                .Select(c => new
                {
                    c.Id,
                    c.AvatarUrl
                }).ToDictionary(c=>c.Id,c=>c.AvatarUrl);
            int pageSize = 10;
            int total = query.Count();
            var bookmarkhisstory = query.OrderByDescending(
                c => c.UpdatedAt.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            //DROPDOWN  STORIES
            ViewBag.Storiesbag = _dbcontext.Stories
                .OrderBy(c => c.Title)
                .ToList();
            ViewBag.ToTal = total;
            ViewBag.Page = page;
            ViewBag.PageSize=pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewData["Search"] = searach;
            return View(bookmarkhisstory);
        }
        // USER DETAIL
        public IActionResult UserDetail(Guid id)
        {
            var userBookmarks = _dbcontext.Bookmarks
                .Include(c => c.Story)
                .Include(c => c.Chapter)
                .Include(c => c.User)
                .Where(c => c.UserId == id)
                .OrderByDescending(c => c.UpdatedAt)
                .ToList();

            if (!userBookmarks.Any())
                return NotFound();

            ViewBag.TotalBookmarks = userBookmarks.Count;

            ViewBag.TotalStories = userBookmarks
                .Select(c => c.StoryId)
                .Distinct()
                .Count();

            ViewBag.LastBookmark = userBookmarks.Max(c => c.UpdatedAt);

            return View(userBookmarks);
        }
        // STORY DETAIL
        public IActionResult StoryDetail(Guid id)
        {
            var bookmarks = _dbcontext.Bookmarks
                .Include(c => c.User)
                .Include(c => c.Story)
                .Include(c => c.Chapter)
                .Where(c => c.StoryId == id)
                .OrderByDescending(c => c.UpdatedAt)
                .ToList();

            if (!bookmarks.Any())
                return NotFound();

            ViewBag.TotalBookmarks = bookmarks.Count;

            ViewBag.TotalUsers = bookmarks
                .Select(c => c.UserId)
                .Distinct()
                .Count();

            ViewBag.TotalChapters = bookmarks
                .Where(c => c.ChapterId != null)
                .Select(c => c.ChapterId)
                .Distinct()
                .Count();

            return View(bookmarks);
        }
        // CHART – TOP STORIES
        public JsonResult ChartTopStories()
        {
            var data = _dbcontext.Bookmarks
                .Include(c => c.Story)
                .GroupBy(c => new
                {
                    c.StoryId,
                    c.Story.Title
                })
                .Select(g => new
                {
                    title = g.Key.Title,
                    count = g.Count()
                })
                .OrderByDescending(x => x.count)
                .Take(10)
                .ToList();

            return Json(data);
        }
        // CHART – TOP USERS
        public JsonResult ChartTopUsers()
        {
            var data = _dbcontext.Bookmarks
                .Include(c => c.User)
                .GroupBy(c => new
                {
                    c.UserId,
                    c.User.Username
                })
                .Select(g => new
                {
                    username = g.Key.Username,
                    count = g.Count()
                })
                .OrderByDescending(x => x.count)
                .Take(10)
                .ToList();

            return Json(data);
        }
        // CHART – BOOKMARK TREND (30 ngày)
        public JsonResult ChartBookmarkTrend()
        {
            var last30Days = DateTime.UtcNow.AddDays(-30);

            var data = _dbcontext.Bookmarks
                .Where(c => c.UpdatedAt >= last30Days)
                .GroupBy(c => c.UpdatedAt.Date)
                .Select(g => new
                {
                    date = g.Key,
                    count = g.Count()
                })
                .OrderBy(x => x.date)
                .ToList();

            return Json(data);
        }
        // CHART – HEATMAP (giờ bookmark cao điểm)
        public JsonResult Heatmap()
        {
            var data = _dbcontext.Bookmarks
                .GroupBy(c => c.UpdatedAt.Hour)
                .Select(g => new
                {
                    hour = g.Key,
                    count = g.Count()
                })
                .OrderBy(x => x.hour)
                .ToList();

            return Json(data);
        }

        // CHART – TOP CHAPTERS được bookmark nhiều
        public JsonResult ChartTopChapters()
        {
            var data = _dbcontext.Bookmarks
                .Where(c => c.ChapterId != null)
                .Include(c => c.Story)
                .Include(c => c.Chapter)
                .GroupBy(c => new
                {
                    c.ChapterId,
                    c.Story.Title,
                    c.Chapter.ChapterNumber
                })
                .Select(g => new
                {
                    story = g.Key.Title,
                    chapter = g.Key.ChapterNumber,
                    count = g.Count()
                })
                .OrderByDescending(x => x.count)
                .Take(10)
                .ToList();

            return Json(data);
        }
    }

}