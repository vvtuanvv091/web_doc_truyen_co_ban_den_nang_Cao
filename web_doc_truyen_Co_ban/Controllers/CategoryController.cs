using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_doc_truyen_Co_ban.Data;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace web_doc_truyen_Co_ban.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;
        public CategoryController(ApplicationDbContext context)
        {
            _dbcontext = context;
        }

        //public async Task<IActionResult> Index(int page = 1)
        //{
        //    int pageSize = 12;

        //    var query = _dbcontext.Stories
        //        .Include(x => x.Category)
        //        .Include(x => x.Chapters)
        //        .Include(x => x.Tags)
        //        .AsQueryable();

        //    int totalItems = await query.CountAsync();

        //    var stories = await query
        //        .OrderByDescending(x => x.Id)
        //        .Skip((page - 1) * pageSize)
        //        .Take(pageSize)
        //        .ToListAsync();

        //    ViewBag.CurrentPage = page;
        //    ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        //    return View(stories);
        //}//chú ý này còn làm tiếp để bắt session
        /// <summary>
        //public async Task<IActionResult> ListByCategory(string slug, int page = 1)
        //{
        //    if (string.IsNullOrEmpty(slug))
        //        return RedirectToAction("Index");

        //    int pageSize = 9;

        //    var query = _dbcontext.Stories
        //        .Include(x => x.Category)
        //        .Include(x => x.Tags)
        //        .Where(x => x.Category.Slug == slug);

        //    int totalItems = await query.CountAsync();

        //    var stories = await query
        //        .OrderByDescending(x => x.Id)
        //        .Skip((page - 1) * pageSize)
        //        .Take(pageSize)
        //        .ToListAsync();

        //    ViewBag.CurrentPage = page;
        //    ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        //    ViewBag.Slug = slug;

        //    return View(stories);
        //}

        public async Task<IActionResult> ListByCategory(
        string slug,
        int page = 1,
        string sort = "new",
        string status = "",
        string audio = "",
        string minchap = "0",
        string tag = "",
        string keyword = "")
        {
            if (string.IsNullOrEmpty(slug))
                return RedirectToAction("Index");

            int pageSize = 9;

            var query = _dbcontext.Stories
                .Include(x => x.Category)
                .Include(x => x.Tags)
                .Include(x => x.Chapters)
                .Where(x => x.Category != null && x.Category.Slug == slug)
                .AsQueryable();
            if(!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();

                query = query.Where(x =>
                    x.Title.ToLower().Contains(keyword) ||
                    x.OriginalAuthor.ToLower().Contains(keyword)
                // || x.Alias.ToLower().Contains(keyword)
                // || x.Description.ToLower().Contains(keyword)
                );
            }    

            // ── Filter ──
            if (!string.IsNullOrEmpty(status) && byte.TryParse(status, out byte st))
                query = query.Where(x => x.Status == st);

            //if (audio == "1")
            //    query = query.Where(x => x.HasAudio == true);

            if (int.TryParse(minchap, out int mc) && mc > 0)
                query = query.Where(x => x.Chapters.Count >= mc);

            if (!string.IsNullOrEmpty(tag) && Guid.TryParse(tag, out Guid tagId))
                query = query.Where(x => x.Tags.Any(t => t.Id == tagId));

            // ── Sort ──
            query = sort switch
            {
                "view" => query.OrderByDescending(x => x.TotalViews),
                "rating" => query.OrderByDescending(x => x.RatingAvg),
                _ => query.OrderByDescending(x => x.LastChapterAt)
            };

            int totalItems = await query.CountAsync();

            var stories = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.Slug = slug;

            return View("Index", stories);
        }
        public async Task<IActionResult> Index(
        int page = 1,
        string sort = "new",
        string status = "",
        string audio = "",
        string minchap = "0",
        string category = "",
        string tag = "",
        string keyword = "")
        {
            int pageSize = 12;

            var query = _dbcontext.Stories
                .Include(x => x.Category)
                .Include(x => x.Chapters)
                .Include(x => x.Tags)
                .AsQueryable();

            // ── Lọc trạng thái ──
            if (!string.IsNullOrEmpty(status) && byte.TryParse(status, out byte st))
                query = query.Where(x => x.Status == st);

            //// ── Lọc audio ──
            //if (audio == "1")
            //    query = query.Where(x => x.HasAudio == true);
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();

                query = query.Where(x =>
                    x.Title.ToLower().Contains(keyword) ||
                    x.OriginalAuthor.ToLower().Contains(keyword)
                // || x.Alias.ToLower().Contains(keyword)
                // || x.Description.ToLower().Contains(keyword)
                );
            }
            // ── Lọc số chương tối thiểu ──
            if (int.TryParse(minchap, out int mc) && mc > 0)
                query = query.Where(x => x.Chapters.Count >= mc);

            // ── Lọc thể loại ──
            if (!string.IsNullOrEmpty(category) && Guid.TryParse(category, out Guid catId))
                query = query.Where(x => x.CategoryId == catId);

            // ── Lọc tag ──
            if (!string.IsNullOrEmpty(tag) && Guid.TryParse(tag, out Guid tagId))
                query = query.Where(x => x.Tags.Any(t => t.Id == tagId));

            // ── Sắp xếp ──
            query = sort switch
            {
                "view" => query.OrderByDescending(x => x.TotalViews),
                "rating" => query.OrderByDescending(x => x.RatingAvg),
                //"nominate" => query.OrderByDescending(x => x.NominateCount),
                _ => query.OrderByDescending(x => x.LastChapterAt) // "new" mặc định
            };

            int totalItems = await query.CountAsync();

            var stories = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            ViewBag.Keyword = keyword;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View(stories);
        }
    }
}
