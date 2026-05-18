//using DocumentFormat.OpenXml.ExtendedProperties;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using web_doc_truyen_Co_ban.Data;

//namespace web_doc_truyen_Co_ban.Areas.Admin.Controllers
//{
//    public class ReadHistoryController : Controller
//    {
//        private readonly ApplicationDbContext _dbcontext;  
//        public ReadHistoryController(ApplicationDbContext db)
//        {
//            _dbcontext = db;
//        }   

//        public IActionResult Index(string? search,string? storyId,int page=1)
//        {
//            ViewData["ActiveMenu"] = "ReadHistory";
//            var query = _dbcontext.ReadingHistories
//                .Include(r => r.User)
//                .Include(r => r.Story)
//                .AsQueryable();
//            if(!string.IsNullOrWhiteSpace(search))
//            {
//                search = search.Trim().ToLower();
//                query = query.Where(r => r.User.Username.ToLower().Contains(search));
//            }    
//            if(!string.IsNullOrWhiteSpace(storyId)&&Guid.TryParse(storyId,out var sid))
//            {
//                query = query.Where(r => r.StoryId == sid);
//                ViewData["StoryId"] = storyId;
//            }
//             int pageSize = 15;
//             int total = query.Count();
//             var histories = query
//                .OrderByDescending(r => r.LastReadAt)
//                .Skip((page - 1) * pageSize)
//                .Take(pageSize)
//                .ToList();
//             ViewData["Histories"] = histories;
//             ViewData["TotalPages"] = (int)Math.Ceiling(total / (double)pageSize);
//             ViewData["CurrentPage"] = page;
//            return View(histories);
//        }

//    }
//}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_doc_truyen_Co_ban.Data;

namespace web_doc_truyen_Co_ban.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ReadHistoryController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;

        public ReadHistoryController(ApplicationDbContext db)
        {
            _dbcontext = db;
        }
        public IActionResult Index(
            string? search,
            string? storyId,
            int page = 1)
        {
            ViewData["ActiveMenu"] = "ReadHistory";

            var query = _dbcontext.ReadingHistories
                .Include(r => r.User)
                .Include(r => r.Story)
                .Include(r => r.Chapter)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();

                query = query.Where(r =>
                    r.User.Username.ToLower().Contains(search));
            }

            // filter story
            if (!string.IsNullOrWhiteSpace(storyId)
                && Guid.TryParse(storyId, out var sid))
            {
                query = query.Where(r => r.StoryId == sid);

                ViewData["StoryId"] = storyId;
            }

            ViewBag.TotalReads = _dbcontext.ReadingHistories.Count();

            ViewBag.TotalUsers =
                _dbcontext.ReadingHistories
                    .Select(r => r.UserId)
                    .Distinct()
                    .Count();

            ViewBag.TotalStories =
                _dbcontext.ReadingHistories
                    .Select(r => r.StoryId)
                    .Distinct()
                    .Count();

            ViewBag.TodayReads =
                _dbcontext.ReadingHistories
                    .Count(r => r.LastReadAt.Date == DateTime.UtcNow.Date);
            ViewBag.imgAvartarUrl = _dbcontext.Users.Select(u => new { u.Id, u.AvatarUrl }).ToDictionary(u => u.Id, u => u.AvatarUrl);

            int pageSize = 20;

            int total = query.Count();

            var histories = query
                .OrderByDescending(r => r.LastReadAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            ViewBag.Stories = _dbcontext.Stories
                .OrderBy(s => s.Title)
                .ToList();

            ViewBag.Total = total;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages =
                (int)Math.Ceiling(total / (double)pageSize);

            ViewData["Search"] = search;

            return View(histories);
        }
        public IActionResult UserDetail(Guid id)
        {
            var userReads = _dbcontext.ReadingHistories
                .Include(r => r.Story)
                .Include(r => r.Chapter)
                .Include(r => r.User)
                .Where(r => r.UserId == id)
                .OrderByDescending(r => r.LastReadAt)
                .ToList();

            if (!userReads.Any())
                return NotFound();

            ViewBag.TotalReads = userReads.Count;

            ViewBag.TotalStories =
                userReads.Select(r => r.StoryId)
                         .Distinct()
                         .Count();

            ViewBag.LastRead =
                userReads.Max(r => r.LastReadAt);

            return View(userReads);
        }
        public IActionResult StoryDetail(Guid id)
        {
            var reads = _dbcontext.ReadingHistories
                .Include(r => r.User)
                .Include(r => r.Story)
                .Include(r => r.Chapter)
                .Where(r => r.StoryId == id)
                .OrderByDescending(r => r.LastReadAt)
                .ToList();

            if (!reads.Any())
                return NotFound();

            ViewBag.TotalReads = reads.Count;

            ViewBag.TotalUsers =
                reads.Select(r => r.UserId)
                     .Distinct()
                     .Count();

            ViewBag.AvgChapter =
                reads.Average(r => r.ChapterNumber ?? 0);

            return View(reads);
        }
        public JsonResult ChartTopStories()
        {
            var data = _dbcontext.ReadingHistories
                .Include(r => r.Story)
                .GroupBy(r => new
                {
                    r.StoryId,
                    r.Story.Title
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
        public JsonResult ChartTopUsers()
        {
            var data = _dbcontext.ReadingHistories
                .Include(r => r.User)
                .GroupBy(r => new
                {
                    r.UserId,
                    r.User.Username
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
        public JsonResult ChartReadingTrend()
        {
            var last30Days = DateTime.UtcNow.AddDays(-30);

            var data = _dbcontext.ReadingHistories
                .Where(r => r.LastReadAt >= last30Days)
                .GroupBy(r => r.LastReadAt.Date)
                .Select(g => new
                {
                    date = g.Key,
                    count = g.Count()
                })
                .OrderBy(x => x.date)
                .ToList();

            return Json(data);
        }
        public JsonResult ChartChapterReads()
        {
            var data = _dbcontext.ReadingHistories
                .Where(r => r.ChapterNumber != null)
                .GroupBy(r => new
                {
                    r.Story.Title,
                    r.ChapterNumber
                })
                .Select(g => new
                {
                    story = g.Key.Title,
                    chapter = g.Key.ChapterNumber,
                    count = g.Count()
                })
                .OrderByDescending(x => x.count)
                .Take(20)
                .ToList();

            return Json(data);
        }
        public JsonResult Heatmap()
        {
            var data = _dbcontext.ReadingHistories
                .GroupBy(r => r.LastReadAt.Hour)
                .Select(g => new
                {
                    hour = g.Key,
                    count = g.Count()
                })
                .OrderBy(x => x.hour)
                .ToList();

            return Json(data);
        }
    }
}