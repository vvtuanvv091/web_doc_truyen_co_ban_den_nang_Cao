using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Buffers.Text;
using System.Text.Json;
using web_doc_truyen_Co_ban.Areas.Admin.Service;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Models;
using static System.Net.WebRequestMethods;

namespace web_doc_truyen_Co_ban.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ImportController : Controller
    {
        private readonly MangaDexService _mangaDex;
        private readonly ApplicationDbContext _dbcontext;
        private readonly IWebHostEnvironment _env;
        public ImportController(MangaDexService mangaDex, ApplicationDbContext db, IWebHostEnvironment env)
        {
            _mangaDex = mangaDex;
            _dbcontext = db;
            _env = env;
        }

        // ─── GET /Admin/Import ──────────────────────────────────────────────────
        // Trang tìm kiếm trên MangaDex
        [HttpGet]
        public IActionResult Index()
        {
            ViewData["ActiveMenu"] = "Import";
            return View();
        }

        // ─── POST /Admin/Import/Search ──────────────────────────────────────────
        // Tìm theo tên → hiện danh sách kết quả
        [HttpPost]
        public async Task<IActionResult> Search(string title)
        {
            ViewData["ActiveMenu"] = "Import";
            ViewData["SearchTitle"] = title;

            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["Error"] = "Vui lòng nhập tên truyện cần tìm.";
                return View("Index");
            }

            var results = await _mangaDex.SearchAsync(title, limit: 20);
            return View("~/Areas/Admin/Views/Import/SearchResults.cshtml", results);
        }

        [HttpGet]
        public async Task<IActionResult> Preview(string mangaDexId)
        {
            ViewData["ActiveMenu"] = "Import";

            var result = await _mangaDex.GetByIdAsync(mangaDexId);
            if (result == null)
            {
                TempData["Error"] = "Không lấy được thông tin từ MangaDex.";
                return RedirectToAction("Index");
            }

            var story = _mangaDex.MapToStoryModel(result);

            ViewBag.MangaDexId = mangaDexId;
            ViewBag.Tags = result.Tags;
            ViewBag.Categories = new SelectList(_dbcontext.Categories, "Id", "Name");
            ViewBag.AllTags = new MultiSelectList(_dbcontext.Tags, "Id", "Name");

            // ← Phải lấy chapters TRƯỚC khi return
            var chapters = await _mangaDex.GetChaptersAsync(mangaDexId, Guid.Empty);
            ViewBag.Chapters = chapters;

            return View(story); // ← return phải ở CUỐI
        }

        // ─── POST /Admin/Import/DoImport ────────────────────────────────────────
        // Thực sự lưu vào DB
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoImport(
            string mangaDexId,
            Guid categoryId,
            List<Guid>? selectedTags)
        {
            // Kiểm tra trùng
            bool existed = await _dbcontext.Stories.AnyAsync(s => s.Source == $"https://mangadex.org/title/{mangaDexId}");
            if (existed)
            {
                TempData["Error"] = "Truyện này đã được import rồi!";
                return RedirectToAction("Index");
            }

            var result = await _mangaDex.GetByIdAsync(mangaDexId);
            if (result == null)
            {
                TempData["Error"] = "Không lấy được dữ liệu từ MangaDex.";
                return RedirectToAction("Index");
            }

            var story = _mangaDex.MapToStoryModel(result);
            story.Id = Guid.NewGuid();
            story.CategoryId = categoryId;
            story.CoverImageUrl = await _mangaDex.DownloadCoverAsync(story.CoverImageUrl, _env);
            // Gắn tags đã chọn
            if (selectedTags != null && selectedTags.Any())
            {
                var tags = await _dbcontext.Tags
                    .Where(t => selectedTags.Contains(t.Id))
                    .ToListAsync();
                story.Tags = tags;
            }

            _dbcontext.Stories.Add(story);
            var chapters = await _mangaDex.GetChaptersAsync(mangaDexId, story.Id);
            if (chapters.Any())
                await _dbcontext.Chapters.AddRangeAsync(chapters);
            await _dbcontext.SaveChangesAsync();

            TempData["Success"] = $"Import thành công: \"{story.Title}\"";
            return RedirectToAction("Index", "Story", new { area = "Admin" });
        }
        // ─── Get Chapters by MangaDex Manga ID ─────────────────────────────────────

    }
}
