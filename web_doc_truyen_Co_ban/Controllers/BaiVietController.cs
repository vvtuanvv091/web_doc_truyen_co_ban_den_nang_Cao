using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Models;

namespace web_doc_truyen_Co_ban.Controllers
{
    [Authorize]
    public class BaiVietController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;

        public BaiVietController(ApplicationDbContext context)
        {
            _dbcontext = context;
        }

        private Guid? GetCurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : null;
        }

        // -------------------------------------------------------
        // INDEX
        // -------------------------------------------------------
        [AllowAnonymous]
        public async Task<IActionResult> Index(Guid? categoryId, string? search, int page = 1)
        {
            int pageSize = 15;

            var query = _dbcontext.Threads
                .Include(t => t.ForumCategory)
                .Include(t => t.User)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(t => t.ForumCategoryId == categoryId.Value);
                ViewBag.CurrentCategory = await _dbcontext.ForumCategory.FindAsync(categoryId.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(t => t.Title.ToLower().Contains(search));
                ViewData["Search"] = search;
            }

            int total = await query.CountAsync();

            var threads = await query
                .OrderByDescending(t => t.IsPinned)
                .ThenByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Categories = await _dbcontext.ForumCategory
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            ViewBag.CategoryId = categoryId;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

            return View(threads);
        }

        // -------------------------------------------------------
        // DETAIL
        // -------------------------------------------------------
        [AllowAnonymous]
        public async Task<IActionResult> Detail(Guid id)
        {
            var thread = await _dbcontext.Threads
                .Include(t => t.ForumCategory)
                .Include(t => t.User)
                .Include(t => t.Replies.OrderBy(r => r.CreatedAt))
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (thread == null) return NotFound();

            var userId = GetCurrentUserId();
            bool isOwner = userId.HasValue && thread.UserId == userId.Value;
            bool isAdmin = User.IsInRole("Admin");

            thread.ViewCount++;
            await _dbcontext.SaveChangesAsync();

            ViewBag.IsOwner = isOwner;
            ViewBag.IsAdmin = isAdmin;
            ViewBag.CurrentUserId = userId;

            return View(thread);
        }

        // -------------------------------------------------------
        // CREATE GET
        // -------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Create(Guid? categoryId)
        {
            ViewBag.Categories = new SelectList(
                await _dbcontext.ForumCategory.OrderBy(c => c.DisplayOrder).ToListAsync(),
                "Id", "Name", categoryId
            );
            ViewBag.DefaultCategoryId = categoryId;
            return View();
        }

        // -------------------------------------------------------
        // CREATE POST
        // -------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BaiVietModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var category = await _dbcontext.ForumCategory.FindAsync(model.ForumCategoryId);
            if (category == null)
                ModelState.AddModelError("ForumCategoryId", "Danh mục không tồn tại");

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(
                    await _dbcontext.ForumCategory.OrderBy(c => c.DisplayOrder).ToListAsync(),
                    "Id", "Name", model.ForumCategoryId
                );
                return View(model);
            }

            model.Id = Guid.NewGuid();
            model.UserId = userId.Value;
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;
            model.ViewCount = 0;
            model.ReplyCount = 0;
            model.IsPinned = false;
            model.IsLocked = false;

            _dbcontext.Threads.Add(model);
            await _dbcontext.SaveChangesAsync();

            TempData["Success"] = "Đã đăng bài viết thành công!";
            return RedirectToAction("Detail", new { id = model.Id });
        }

        // -------------------------------------------------------
        // EDIT GET
        // -------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var thread = await _dbcontext.Threads.FindAsync(id);
            if (thread == null) return NotFound();

            if (thread.UserId != userId.Value && !User.IsInRole("Admin"))
                return Forbid();

            ViewBag.Categories = new SelectList(
                await _dbcontext.ForumCategory.OrderBy(c => c.DisplayOrder).ToListAsync(),
                "Id", "Name", thread.ForumCategoryId
            );

            return View(thread);
        }

        // -------------------------------------------------------
        // EDIT POST
        // -------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, BaiVietModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var thread = await _dbcontext.Threads.FindAsync(id);
            if (thread == null) return NotFound();

            if (thread.UserId != userId.Value && !User.IsInRole("Admin"))
                return Forbid();

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(
                    await _dbcontext.ForumCategory.OrderBy(c => c.DisplayOrder).ToListAsync(),
                    "Id", "Name", model.ForumCategoryId
                );
                return View(model);
            }

            thread.Title = model.Title;
            thread.Content = model.Content;
            thread.ForumCategoryId = model.ForumCategoryId;
            thread.UpdatedAt = DateTime.UtcNow;

            await _dbcontext.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật bài viết thành công!";
            return RedirectToAction("Detail", new { id = thread.Id });
        }

        // -------------------------------------------------------
        // DELETE
        // -------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var thread = await _dbcontext.Threads.FindAsync(id);
            if (thread == null) return NotFound();

            if (thread.UserId != userId.Value && !User.IsInRole("Admin"))
                return Forbid();

            var categoryId = thread.ForumCategoryId;

            _dbcontext.Threads.Remove(thread);
            await _dbcontext.SaveChangesAsync();

            TempData["Success"] = "Đã xóa bài viết.";
            return RedirectToAction("Index", new { categoryId });
        }
    }
}