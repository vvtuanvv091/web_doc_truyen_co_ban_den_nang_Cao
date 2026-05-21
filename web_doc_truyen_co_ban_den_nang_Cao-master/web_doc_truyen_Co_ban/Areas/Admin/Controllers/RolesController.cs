using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Models;

namespace web_doc_truyen_Co_ban.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]/[action]")]
    public class RolesController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public RolesController(ApplicationDbContext dbcontext, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _dbcontext = dbcontext;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        
        public async Task<IActionResult> Index(string? search,int page=1)
        {
            ViewData["ActiveMenu"] = "Roles";
            var query = _dbcontext.Roles.AsQueryable();
            int pagesize = 10;
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query=query.Where(r=> r.Name.ToLower().Contains(search));
                ViewData["Search"] = search;
            }
            int total = await query.CountAsync();
            var roles = await query
                        .OrderBy(r => r.Name)
                        .Skip((page - 1) * pagesize)
                        .Take(pagesize)
                        .ToListAsync();
            var rolecounts = new Dictionary<Guid, int>();//hiểu rằng role.Id là Guid, nhưng trong UserRoles.RoleId là string nên phải chuyển đổi
            foreach (var role in roles)
            {
                rolecounts[role.Id] = await _dbcontext.UserRoles.CountAsync(ur => ur.RoleId == role.Id.ToString());
            }
            ViewBag.Page = page;
            ViewBag.PageSize = pagesize;
            ViewBag.TotalPages= (int)Math.Ceiling((double)total / pagesize);
            ViewBag.RoleCounts = rolecounts;
               
            return View(roles);
        }
        [HttpGet]
        public IActionResult Create()
        {
            ViewData["ActiveMenu"] = "Roles";
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>Create (RolesModel roles)
        {
            if (string.IsNullOrWhiteSpace(roles.Name) || roles.Name.Length < 2)
            {
                ModelState.AddModelError("Name", "Tên quyền phải có ít nhất 2 ký tự.");
                return View(roles);
            }

            bool exists = await _dbcontext.Roles
                .AnyAsync(r => r.Name.ToLower() == roles.Name.Trim().ToLower());
            if (exists)
            {
                ModelState.AddModelError("Name", "Tên quyền đã tồn tại.");
                return View(roles);
            }

            // 1. Tạo IdentityRole
            var identityRole = new IdentityRole(roles.Name.Trim());
            var result = await _roleManager.CreateAsync(identityRole);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", result.Errors.FirstOrDefault()?.Description ?? "Tạo thất bại.");
                return View(roles);
            }

            // 2. Ghi vào dbo.Roles cùng Guid
            var role = new RolesModel
            {
                Id = Guid.Parse(identityRole.Id),
                Name = roles.Name.Trim(),
                Permissions = roles .Permissions?.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            _dbcontext.Roles.Add(role);
            await _dbcontext.SaveChangesAsync();

            TempData["Success"] = "Tạo quyền thành công!";
            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            ViewData["ActiveMenu"] = "Roles";
            var role = await _dbcontext.Roles.FindAsync(id);
            if (role == null) return NotFound();
            return View(role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, RolesModel model)
        {
            var role = await _dbcontext.Roles.FindAsync(id);
            if (role == null) return NotFound();

            if (string.IsNullOrWhiteSpace(model.Name) || model.Name.Length < 2)
            {
                ModelState.AddModelError("Name", "Tên quyền phải có ít nhất 2 ký tự.");
                return View(role);
            }

            bool exists = await _dbcontext.Roles
                .AnyAsync(r => r.Name.ToLower() == model.Name.Trim().ToLower() && r.Id != id);
            if (exists)
            {
                ModelState.AddModelError("Name", "Tên quyền đã tồn tại.");
                return View(role);
            }

            // Cập nhật dbo.Roles
            role.Name = model.Name.Trim();
            role.Permissions = model.Permissions?.Trim();
            await _dbcontext.SaveChangesAsync();

            // Sync IdentityRole
            var identityRole = await _roleManager.FindByIdAsync(id.ToString());
            if (identityRole != null)
            {
                identityRole.Name = model.Name.Trim();
                identityRole.NormalizedName = model.Name.Trim().ToUpper();
                await _roleManager.UpdateAsync(identityRole);
            }

            TempData["Success"] = "Cập nhật quyền thành công!";
            return RedirectToAction("Index");
        }

        // ==================== DELETE ====================
        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            ViewData["ActiveMenu"] = "Roles";
            var role = await _dbcontext.Roles
                .Include(r => r.Users)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (role == null) return NotFound();
            return View(role);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var role = await _dbcontext.Roles
                .Include(r => r.Users)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (role == null) return NotFound();

            // Chặn xóa nếu còn user đang dùng
            if (role.Users.Any())
            {
                TempData["Error"] = $"Không thể xóa — còn {role.Users.Count} người dùng đang dùng quyền này.";
                return RedirectToAction("Index");
            }

            // Xóa IdentityRole
            var identityRole = await _roleManager.FindByIdAsync(id.ToString());
            if (identityRole != null)
                await _roleManager.DeleteAsync(identityRole);

            _dbcontext.Roles.Remove(role);
            await _dbcontext.SaveChangesAsync();

            TempData["Success"] = "Xóa quyền thành công!";
            return RedirectToAction("Index");
        }
    }
}
