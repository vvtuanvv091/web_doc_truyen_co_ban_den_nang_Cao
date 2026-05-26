using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using System.Security.Claims;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace web_doc_truyen_Co_ban.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class StoryController : Controller
    {

        private readonly ApplicationDbContext _dbcontext;
        public StoryController(ApplicationDbContext context)
        {
            _dbcontext = context;
        }

        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            ViewData["ActiveMenu"] = "Story";
            int pageSize = 10;

            var query = _dbcontext.Stories
                .Include(s => s.Category)
                .Include(s => s.Tags)
                .Include(s => s.Author)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(s => s.Title.ToLower().Contains(search)
                                      || s.Slug.ToLower().Contains(search));
                ViewData["Search"] = search;
            }

            int total = await query.CountAsync();
            var stories = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            var storyIds = stories.Select(s => s.Id).ToList();
            // TotalViews
            var totalViews = await _dbcontext.ChapterViews
                .Where(cv => storyIds.Contains(cv.StoryId))
                .GroupBy(cv => cv.StoryId)
                .Select(g => new { StoryId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.StoryId, x => x.Count);

            // TotalChapters
            var totalChapters = await _dbcontext.Chapters
                .Where(c => storyIds.Contains(c.StoryId))
                .GroupBy(c => c.StoryId)
                .Select(g => new { StoryId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.StoryId, x => x.Count);

            // RatingAvg
            var ratingAvg = await _dbcontext.Ratings
                .Where(r => storyIds.Contains(r.StoryId))
                .GroupBy(r => r.StoryId)
                .Select(g => new { StoryId = g.Key, Avg = g.Average(r => r.Score) })
                .ToDictionaryAsync(x => x.StoryId, x => x.Avg);
            ViewBag.TotalViews = totalViews;
            ViewBag.TotalChapters = totalChapters;
            ViewBag.RatingAvg = ratingAvg;

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

            return View(stories);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(_dbcontext.Categories, "Id", "Name");
            ViewBag.Tags = new MultiSelectList(_dbcontext.Tags, "Id", "Name");
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StoryModel storie, IFormFile? coverFile, List<Guid>? selectedTags)
        {
            if (string.IsNullOrWhiteSpace(storie.Slug) && !string.IsNullOrWhiteSpace(storie.Title))
            {
                storie.Slug = storie.Title
                    .ToLower()
                    .Normalize(System.Text.NormalizationForm.FormD)
                    .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                                != System.Globalization.UnicodeCategory.NonSpacingMark)
                    .Aggregate("", (s, c) => s + c)
                    .Replace(" ", "-")
                    .Trim('-');


                ModelState.Remove("Slug");
            }

            // Xử lý upload file nếu có
            if (coverFile != null && coverFile.Length > 0)
            {
                var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(coverFile.FileName).ToLower();

                if (!allowedExt.Contains(ext))
                {
                    ModelState.AddModelError("CoverImageUrl", "Chỉ chấp nhận JPG, PNG, WEBP.");
                }
                else if (coverFile.Length > 10 * 1024 * 1024)
                {
                    ModelState.AddModelError("CoverImageUrl", "File tối đa 10MB.");
                }
                else
                {
                    // Lưu vào wwwroot/img/covers/
                    // Bằng dòng này - không cần inject IWebHostEnvironment
                    var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img");
                    Directory.CreateDirectory(uploadsDir); // tạo thư mục nếu chưa có

                    var fileName = $"{Guid.NewGuid()}{ext}";
                    var filePath = Path.Combine(uploadsDir, fileName);

                    using var stream = new FileStream(filePath, FileMode.Create);
                    await coverFile.CopyToAsync(stream);

                    // Lưu đường dẫn tương đối vào model
                    storie.CoverImageUrl = "img/" + fileName;
                }
            }

            if (storie.CategoryId != Guid.Empty)
            {
                ModelState.Remove("CategoryId");
                ModelState.Remove("Category"); // ✅ xóa cả navigation property
            }

            if (ModelState.IsValid)
            {
                storie.Id = Guid.NewGuid();
                if (selectedTags != null && selectedTags.Any())
                {
                    var tags = await _dbcontext.Tags
                        .Where(t => selectedTags.Contains(t.Id))
                        .ToListAsync();

                    storie.Tags = tags;
                }
                storie.CreatedAt = DateTime.UtcNow;
                storie.UpdatedAt = DateTime.UtcNow;

                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (Guid.TryParse(userIdStr, out Guid userId))
                    storie.AuthorId = userId;

                _dbcontext.Stories.Add(storie);
                await _dbcontext.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            // ✅ Debug: xem lỗi nào đang chặn
            foreach (var err in ModelState)
                if (err.Value.Errors.Any())
                    Console.WriteLine($"[ModelState Error] {err.Key}: {err.Value.Errors[0].ErrorMessage}");

            ViewBag.Categories = new SelectList(_dbcontext.Categories, "Id", "Name");
            return View(storie);
        }
        [HttpGet]
        public IActionResult Edit(Guid id)
        {
            var story = _dbcontext.Stories.Find(id);
            if (story == null) return NotFound();
            ViewBag.Categories = new SelectList(_dbcontext.Categories, "Id", "Name", story.CategoryId);
            ViewBag.Tags = new MultiSelectList(_dbcontext.Tags, "Id", "Name", story.Tags.Select(t => t.Id));
            return View(story);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(StoryModel model, List<Guid>? selectedTags, IFormFile? coverFile)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(_dbcontext.Categories, "Id", "Name", model.CategoryId);
                ViewBag.Tags = new MultiSelectList(_dbcontext.Tags, "Id", "Name", selectedTags);

                return View(model);
            }
            var story = await _dbcontext.Stories
                .Include(s => s.Tags)
                .FirstOrDefaultAsync(s => s.Id == model.Id);

            if (story == null)
                return NotFound();
            // Upload ảnh mới
            if (coverFile != null && coverFile.Length > 0)
            {
                var extension = Path.GetExtension(coverFile.FileName).ToLower();

                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };

                if (!allowed.Contains(extension))
                {
                    ModelState.AddModelError("", "Ảnh không hợp lệ");

                    ViewBag.Categories = new SelectList(_dbcontext.Categories, "Id", "Name", model.CategoryId);
                    ViewBag.Tags = new MultiSelectList(_dbcontext.Tags, "Id", "Name", selectedTags);

                    return View(model);
                }

                var fileName = Guid.NewGuid() + extension;

                var folder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/img/");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await coverFile.CopyToAsync(stream);
                }

                // lưu db
                story.CoverImageUrl = "/img/" + fileName;
            }
            else
            {
                // nếu dùng URL
                story.CoverImageUrl = model.CoverImageUrl;
            }
            // Tags
            story.Tags.Clear();

            if (selectedTags != null && selectedTags.Any())
            {
                var tags = await _dbcontext.Tags
                    .Where(t => selectedTags.Contains(t.Id))
                    .ToListAsync();

                story.Tags = tags;
            }
            // Update data
            story.Title = model.Title;
            story.Slug = model.Slug;
            story.Description = model.Description;
            story.CategoryId = model.CategoryId;
            story.OriginalAuthor = model.OriginalAuthor;
            story.Source = model.Source;
            story.Status = model.Status;
            story.IsFeatured = model.IsFeatured;
            story.IsVip = model.IsVip;
            story.Is18Plus = model.Is18Plus;

            await _dbcontext.SaveChangesAsync();

            return RedirectToAction("Index");
        }
        [HttpGet]
        public IActionResult Delete(Guid id)
        {
            var storie = _dbcontext.Stories.Find(id);

            if(storie == null)   return NotFound();

            return View(storie);
        }
        //[HttpPost, ActionName("Delete")]
        //public async Task<IActionResult> Deleted(Guid id)
        //{
        //    var story = await _dbcontext.Stories.FindAsync(id);
        //    if (story == null) return NotFound();

        //    _dbcontext.Stories.Remove(story);
        //    await _dbcontext.SaveChangesAsync();

        //    return RedirectToAction("Index");
        //}
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> Deleted(Guid id)
        {
            var story = await _dbcontext.Stories.FindAsync(id);
            if (story == null) return NotFound();

            // Xóa các bảng liên quan trước (vì dùng NoAction)
            var follows = _dbcontext.Follows.Where(f => f.StoryId == id);
            _dbcontext.Follows.RemoveRange(follows);

            var bookmarks = _dbcontext.Bookmarks.Where(b => b.StoryId == id);
            _dbcontext.Bookmarks.RemoveRange(bookmarks);

            var ratings = _dbcontext.Ratings.Where(r => r.StoryId == id);
            _dbcontext.Ratings.RemoveRange(ratings);

            var chapterViews = _dbcontext.ChapterViews.Where(cv => cv.StoryId == id);
            _dbcontext.ChapterViews.RemoveRange(chapterViews);

            var readingHistories = _dbcontext.ReadingHistories.Where(rh => rh.StoryId == id);
            _dbcontext.ReadingHistories.RemoveRange(readingHistories);

            var chapterComments = _dbcontext.ChapterComments.Where(cc => cc.StoryId == id);
            _dbcontext.ChapterComments.RemoveRange(chapterComments);

            // Xóa story (Cascade tự xóa: StoryComments, Chapters)
            _dbcontext.Stories.Remove(story);
            await _dbcontext.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}
