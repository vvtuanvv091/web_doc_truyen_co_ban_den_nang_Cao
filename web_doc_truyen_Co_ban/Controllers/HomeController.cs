using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Models;

namespace web_doc_truyen_Co_ban.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<HomeController> _logger;
        public HomeController(ILogger<HomeController> logger,ApplicationDbContext Context)
        {
            _logger=logger;
            _dbContext=Context;
        }
        public IActionResult Index(string keyword="")
        {
            var userId=User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrEmpty(userId))
                return RedirectToAction("Login","Account");
            var stories= _dbContext.Stories
                .Include(s=>s.Category)
                .Include(s=>s.Follows)
                .Include(s=>s.Ratings)
                .Include(s=>s.Chapters)
                .ToList();
            ViewBag.HotStories= _dbContext.Stories
                .OrderByDescending(c=>c.TotalViews+c.RatingSum)
                .Take(10)
                .ToList();
            ViewBag.NewStories=_dbContext.Stories
                .OrderByDescending(c=>c.UpdatedAt)
                .Take(10)
                .ToList();
            ViewBag.CompleteStories = _dbContext.Stories
                .Where(c => c.Status == 2)
                .OrderByDescending(c => c.TotalViews)
                .Take(10)
                .ToList();
            ViewBag.ReadHistory= _dbContext.ReadingHistories
                .Where(h => h.UserId == Guid.Parse(userId))
                .Include(h => h.Story)
                    .ThenInclude(s => s.Category)
                .Include(h => h.Chapter)
                .OrderByDescending(h => h.LastReadAt)
                .Take(4)
                .ToList();
            ViewBag.Categories = _dbContext.Categories.OrderBy(c => c.Name).ToList();
            var storys = _dbContext.Stories.ToList();
            return View(storys);
        }

        public IActionResult Privacy()
        {
            return View();
        }
        public async Task<IActionResult> StatusStory(byte? status, int page = 1, int pageSize = 20)
        {
            var querystatus = _dbContext.Stories
                .Include(s => s.Category)
                .Include(s => s.Tags)
                .AsQueryable();
            if (status.HasValue)
            {
                querystatus = querystatus.Where(x => x.Status == status.Value);
            }
            querystatus = querystatus.OrderByDescending(x => x.UpdatedAt);
            var totalItems = await querystatus.CountAsync();
            var totalPages = await querystatus.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            //bắt đầu bắt ddeems trang thai
            // Status: 0=Bản nháp, 1=Đang ra, 2=Hoàn thành, 3=Tạm dừng, 4=Drop
            ViewBag.Count_All = await _dbContext.Stories.CountAsync();
            ViewBag.Count_Draft = await _dbContext.Stories.CountAsync(x => x.Status == 0);
            ViewBag.Count_Ongoing = await _dbContext.Stories.CountAsync(x => x.Status == 1);
            ViewBag.Count_Done = await _dbContext.Stories.CountAsync(x => x.Status == 2);
            ViewBag.Count_Pause = await _dbContext.Stories.CountAsync(x => x.Status == 3);
            ViewBag.Count_Drop = await _dbContext.Stories.CountAsync(x => x.Status == 4);

            ViewBag.CurrentStatus = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.TotalItems = totalItems;

            return View(totalPages);
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
