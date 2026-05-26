using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Models;

namespace web_doc_truyen_Co_ban.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ThaoLuanDanhMucController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;

        public ThaoLuanDanhMucController(ApplicationDbContext context)
        {
            _dbcontext = context;
        }

        // -------------------------------------------------------
        // INDEX — Danh sách danh mục thảo luận + tìm kiếm + phân trang
        // -------------------------------------------------------
        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            ViewData["ActiveMenu"] = "ThaoLuanDanhMuc";
            int pageSize = 10;

            var query = _dbcontext.ForumCategory.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(search)
                                      || c.Slug.ToLower().Contains(search));
                ViewData["Search"] = search;
            }

            int total = await query.CountAsync();

            var categories = await query
                .OrderBy(c => c.DisplayOrder)
                .ThenByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new ThaoLuanDanhMucModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    Description = c.Description,
                    DisplayOrder = c.DisplayOrder,
                    CreatedAt = c.CreatedAt,
                    // Đếm số bài viết trong danh mục
                    Threads = c.Threads
                })
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

            return View(categories);
        }

        // -------------------------------------------------------
        // CREATE
        // -------------------------------------------------------
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ThaoLuanDanhMucModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Kiểm tra slug trùng
            bool existSlug = await _dbcontext.ForumCategory
                .AnyAsync(x => x.Slug == model.Slug);

            if (existSlug)
            {
                ModelState.AddModelError("Slug", "Slug đã tồn tại, vui lòng chọn slug khác");
                return View(model);
            }

            model.Id = Guid.NewGuid();
            model.CreatedAt = DateTime.UtcNow;

            _dbcontext.ForumCategory.Add(model);
            await _dbcontext.SaveChangesAsync();

            TempData["Success"] = $"Đã tạo danh mục \"{model.Name}\" thành công!";
            return RedirectToAction("Index");
        }

        // -------------------------------------------------------
        // EDIT
        // -------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var category = await _dbcontext.ForumCategory.FindAsync(id);
            if (category == null)
                return NotFound();

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, ThaoLuanDanhMucModel model)
        {
            if (id != model.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            // Kiểm tra slug trùng (loại trừ chính nó)
            bool existSlug = await _dbcontext.ForumCategory
                .AnyAsync(x => x.Slug == model.Slug && x.Id != model.Id);

            if (existSlug)
            {
                ModelState.AddModelError("Slug", "Slug đã tồn tại, vui lòng chọn slug khác");
                return View(model);
            }

            var category = await _dbcontext.ForumCategory.FindAsync(id);
            if (category == null)
                return NotFound();

            category.Name = model.Name;
            category.Slug = model.Slug;
            category.Description = model.Description;
            category.DisplayOrder = model.DisplayOrder;

            _dbcontext.ForumCategory.Update(category);
            await _dbcontext.SaveChangesAsync();

            TempData["Success"] = $"Đã cập nhật danh mục \"{category.Name}\" thành công!";
            return RedirectToAction("Index");
        }

        // -------------------------------------------------------
        // DELETE
        // -------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var category = await _dbcontext.ForumCategory
                .Include(c => c.Threads)   // Hiển thị số bài viết sẽ bị xóa theo
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var category = await _dbcontext.ForumCategory.FindAsync(id);
            if (category == null)
                return NotFound();

            _dbcontext.ForumCategory.Remove(category);
            await _dbcontext.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa danh mục \"{category.Name}\" (cascade xóa luôn bài viết bên trong).";
            return RedirectToAction("Index");
        }
    }
}