using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Models;

namespace web_doc_truyen_Co_ban.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class TagController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;
        public TagController(ApplicationDbContext context)
        {
            _dbcontext = context;
        }
        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            ViewData["ActiveMenu"] = "Tag";
            int pageSize = 10;

            var query = _dbcontext.Tags.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(t => t.Name.ToLower().Contains(search)
                                       || t.Slug.ToLower().Contains(search));
                ViewData["Search"] = search;
            }

            int total = await query.CountAsync();

            var tags = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TagModel
                {
                    Id = t.Id,
                    Name = t.Name,
                    Slug = t.Slug,
                    CreatedAt = t.CreatedAt,
                    // Đếm số truyện thực tế liên kết
                    StoryCount = t.Stories.Count()
                })
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

            return View(tags);
        }
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Tags = _dbcontext.Tags.ToList();
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TagModel tag)
        {
            if (!ModelState.IsValid)
            {
                return View(tag);
            }
           var existSlug = _dbcontext.Tags
                .Where(t => t.Slug == tag.Slug)
                .FirstOrDefault();
            if (existSlug != null)
            {
                ModelState.AddModelError("Slug", "Slug đã tồn tại.");
                return View(tag);
            }
            tag.Id = Guid.NewGuid();
            tag.CreatedAt = DateTime.Now;
            _dbcontext.Tags.Add(tag);
            await _dbcontext.SaveChangesAsync();
            return RedirectToAction("Index");   
        }
        [HttpGet]
        public IActionResult Edit(Guid id)
        {
            if (id == null){
                return NotFound();
            }
            var tag = _dbcontext.Tags.Find(id);
            if (tag == null)
            {
                return NotFound();
            }
            return View(tag);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(Guid id, TagModel tag)
        {
            if (id != tag.Id)
            {
                return NotFound();
            }
            if (!ModelState.IsValid)
            {
                return View(tag);
            }
            var existSlug = _dbcontext.Tags
                .Where(t => t.Slug == tag.Slug && t.Id != tag.Id)
                .FirstOrDefault();
            if (existSlug != null)
            {
                ModelState.AddModelError("Slug", "Slug đã tồn tại.");
                return View(tag);
            }
            var tags = await _dbcontext.Tags.FindAsync(id);
            if (tags == null)
            {
                return NotFound();
            }
            tags.Name = tag.Name;
            tags.Slug = tag.Slug;
            tags.StoryCount = tag.StoryCount;
            _dbcontext.Tags.Update(tags);
            await _dbcontext.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        [HttpGet]
        public IActionResult Delete(Guid id)
        {
            var tag = _dbcontext.Tags.Find(id);
            if (tag == null)
            {
                return NotFound();
            }
            return View(tag);
        }
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var tag = _dbcontext.Tags.Find(id);
            if (tag == null)
            {
                return NotFound();
            }
            _dbcontext.Remove(tag);
            await _dbcontext.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}
