using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Models;
using web_doc_truyen_Co_ban.Areas.Admin.Service;
using Microsoft.AspNetCore.Authorization;

namespace web_doc_truyen_Co_ban.Areas.Admin.Controllers
{
    [Area("Admin")]
    //[Route("Admin/[controller]/[action]")]
    //[Authorize(Roles = "Admin",ten)]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;
        //private readonly MangaDexService _maadexService;
        public CategoryController(ApplicationDbContext context)
        {
            _dbcontext = context;
        }

        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            ViewData["ActiveMenu"] = "Category";
            int pageSize = 10;

            var query = _dbcontext.Categories.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(search)
                                      || c.Slug.ToLower().Contains(search));
                ViewData["Search"] = search;
            }

            int total = await query.CountAsync();

            var categories = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CategoryModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    Description = c.Description,
                    DisplayOrder = c.DisplayOrder,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    // Đếm số truyện thực tế liên kết
                    StoryCount = c.Stories.Count()
                })
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

            return View(categories);
        }
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Categories = _dbcontext.Categories.ToList();
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryModel category)
        {
            if (!ModelState.IsValid)
            {
                return View(category);
            }
            var existSlug = _dbcontext.Categories
                .Any(x => x.Slug == category.Slug);

            if (existSlug)
            {
                ModelState.AddModelError("Slug", "Slug đã tồn tại");
                return View(category);
            }
            category.Id = Guid.NewGuid();
            category.CreatedAt = DateTime.Now;
            category.UpdatedAt = DateTime.Now;
            category.StoryCount = 0;

            _dbcontext.Categories.Add(category);
            await _dbcontext.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        [HttpGet]
        public IActionResult Edit(Guid id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var theloai = _dbcontext.Categories.Find(id);
            return View(theloai);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, CategoryModel category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }
            if (!ModelState.IsValid)
            {
                return View(category);
            }
            var existSlug = _dbcontext.Categories
                .Any(x => x.Slug == category.Slug && x.Id != category.Id);
            if (existSlug)
            {
                ModelState.AddModelError("Slug", "Slug đã tồn tại");
                return View(category);
            }
            var theloai = _dbcontext.Categories.Find(id);
            if (theloai == null)
            {
                return NotFound();
            }
            theloai.Name = category.Name;
            theloai.Slug = category.Slug;
            theloai.Description = category.Description;
            theloai.StoryCount = category.StoryCount;
            theloai.DisplayOrder = category.DisplayOrder;
            theloai.UpdatedAt = DateTime.Now;
            theloai.IsActive = category.IsActive;   
            _dbcontext.Categories.Update(theloai);
            await _dbcontext.SaveChangesAsync();
            return RedirectToAction("Index");

        }
        [HttpGet]
        public IActionResult Delete(Guid id)
        {
            var theloaicategories = _dbcontext.Categories.Find(id);
            if (theloaicategories == null)
            {
                return NotFound();
            }
            return View(theloaicategories);
        }
        [HttpPost,ActionName("Delete")]
        public async Task<IActionResult>DeleteConfirmed(Guid id)
        {
            var theloaicategories = _dbcontext.Categories.Find(id); 
            if (theloaicategories == null)
            {
                return NotFound();
            }    
            _dbcontext.Remove(theloaicategories);
            await _dbcontext.SaveChangesAsync();

            return RedirectToAction("Index");
        }
        
    }

}
