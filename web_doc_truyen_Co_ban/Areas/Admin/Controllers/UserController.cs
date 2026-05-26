using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Models;

namespace web_doc_truyen_Co_ban.Areas.Admin.Controllers
{
    [Area("Admin")]
    // [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController(
            ApplicationDbContext db,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _dbcontext = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // ==================== INDEX ====================
        public async Task<IActionResult> Index(string? search, Guid? roleFilter, int page = 1)
        {
            ViewData["ActiveMenu"] = "Users";
            int pageSize = 10;

            var query = _dbcontext.Users
                .Include(u => u.Role)   // load RolesModel luôn
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(u =>
                    u.Username.ToLower().Contains(search) ||
                    u.Email.ToLower().Contains(search));
                ViewData["Search"] = search;
            }

            if (roleFilter.HasValue)
                query = query.Where(u => u.RoleId == roleFilter);

            int total = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
            ViewBag.AllRoles = await _dbcontext.Roles.OrderBy(r => r.Name).ToListAsync();
            ViewBag.RoleFilter = roleFilter;

            return View(users);  // Model: List<UsersModel>, mỗi item có .Role navigation
        }

        // ==================== CREATE ====================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.AllRoles = await _dbcontext.Roles.OrderBy(r => r.Name).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || dto.Username.Length < 3)
                return Json(new { success = false, message = "Tên đăng nhập phải có ít nhất 3 ký tự." });

            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
                return Json(new { success = false, message = "Mật khẩu phải có ít nhất 6 ký tự." });

            bool usernameExists = await _dbcontext.Users
                .AnyAsync(u => u.Username.ToLower() == dto.Username.Trim().ToLower());
            if (usernameExists)
                return Json(new { success = false, message = "Tên đăng nhập đã tồn tại." });

            // Lấy RolesModel được chọn
            RolesModel? customRole = null;
            if (dto.RoleId.HasValue)
            {
                customRole = await _dbcontext.Roles.FindAsync(dto.RoleId.Value);
                if (customRole == null)
                    return Json(new { success = false, message = "Quyền không tồn tại." });
            }

            // 1. Tạo IdentityUser
            var identityUser = new IdentityUser
            {
                UserName = dto.Email.Trim().ToLower(),
                Email = dto.Email.Trim().ToLower(),
                EmailConfirmed = true
            };
            var result = await _userManager.CreateAsync(identityUser, dto.Password);
            if (!result.Succeeded)
            {
                var err = result.Errors.FirstOrDefault()?.Description ?? "Tạo thất bại.";
                if (err.Contains("already taken") || err.Contains("is already"))
                    err = "Email này đã được sử dụng.";
                return Json(new { success = false, message = err });
            }

            // 2. Gán IdentityRole (sync với RolesModel)
            if (customRole != null && await _roleManager.RoleExistsAsync(customRole.Name))
                await _userManager.AddToRoleAsync(identityUser, customRole.Name);

            // 3. Ghi vào dbo.Users
            var user = new UsersModel
            {
                Id = Guid.Parse(identityUser.Id),
                Username = dto.Username.Trim(),
                Email = dto.Email.Trim().ToLower(),
                PasswordHash = identityUser.PasswordHash!,
                DisplayName = dto.DisplayName?.Trim() ?? dto.Username.Trim(),
                RoleId = customRole?.Id,         // FK → RolesModel
                Status = 1,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbcontext.Users.Add(user);
            await _dbcontext.SaveChangesAsync();

            return Json(new { success = true, message = "Tạo tài khoản thành công!" });
        }

        // ==================== EDIT ====================
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var user = await _dbcontext.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            ViewBag.AllRoles = await _dbcontext.Roles.OrderBy(r => r.Name).ToListAsync();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, EditUserDto dto)
        {
            var user = await _dbcontext.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return Json(new { success = false, message = "Không tìm thấy user." });

            // Lấy role mới
            RolesModel? newRole = null;
            if (dto.RoleId.HasValue)
            {
                newRole = await _dbcontext.Roles.FindAsync(dto.RoleId.Value);
                if (newRole == null)
                    return Json(new { success = false, message = "Quyền không tồn tại." });
            }

            // Cập nhật UsersModel
            user.DisplayName = dto.DisplayName?.Trim() ?? user.DisplayName;
            user.RoleId = newRole?.Id;
            user.UpdatedAt = DateTime.UtcNow;
            await _dbcontext.SaveChangesAsync();

            // Sync IdentityRole
            var identityUser = await _userManager.FindByIdAsync(id.ToString());
            if (identityUser != null)
            {
                var currentRoles = await _userManager.GetRolesAsync(identityUser);
                await _userManager.RemoveFromRolesAsync(identityUser, currentRoles);

                if (newRole != null && await _roleManager.RoleExistsAsync(newRole.Name))
                    await _userManager.AddToRoleAsync(identityUser, newRole.Name);
            }

            return Json(new { success = true, message = "Cập nhật thành công!" });
        }

        // ==================== DELETE ====================
        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var user = await _dbcontext.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();
            return View(user);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var user = await _dbcontext.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Lấy storyIds và chapterIds của user
            var storyIds = await _dbcontext.Stories
                .Where(x => x.AuthorId == id)
                .Select(x => x.Id)
                .ToListAsync();

            var chapterIds = await _dbcontext.Chapters
                .Where(x => storyIds.Contains(x.StoryId))
                .Select(x => x.Id)
                .ToListAsync();

            // Xóa bảng con của Chapters
            await _dbcontext.Bookmarks.Where(x => chapterIds.Contains(x.Id)).ExecuteDeleteAsync();
            await _dbcontext.ChapterComments.Where(x => chapterIds.Contains(x.ChapterId)).ExecuteDeleteAsync();
            await _dbcontext.ChapterViews.Where(x => chapterIds.Contains(x.ChapterId)).ExecuteDeleteAsync();
            await _dbcontext.ReadingHistories.Where(x => chapterIds.Contains(x.Id)).ExecuteDeleteAsync();
            await _dbcontext.UnlockedChapters.Where(x => chapterIds.Contains(x.ChapterId)).ExecuteDeleteAsync();

            // Xóa bảng con của Stories
            await _dbcontext.ReadingHistories.Where(x => storyIds.Contains(x.StoryId)).ExecuteDeleteAsync();
            await _dbcontext.StoryComments.Where(x => storyIds.Contains(x.StoryId)).ExecuteDeleteAsync();
            await _dbcontext.Follows.Where(x => storyIds.Contains(x.StoryId)).ExecuteDeleteAsync();
            await _dbcontext.Bookmarks.Where(x => storyIds.Contains(x.StoryId)).ExecuteDeleteAsync();
            await _dbcontext.Ratings.Where(x => storyIds.Contains(x.StoryId)).ExecuteDeleteAsync();

            // Xóa Chapters rồi Stories
            await _dbcontext.Chapters.Where(x => storyIds.Contains(x.StoryId)).ExecuteDeleteAsync();
            await _dbcontext.Stories.Where(x => x.AuthorId == id).ExecuteDeleteAsync();

            // NoAction - xóa thủ công
            await _dbcontext.StoryComments.Where(x => x.UserId == id).ExecuteDeleteAsync();
            await _dbcontext.ChapterComments.Where(x => x.UserId == id).ExecuteDeleteAsync();
            await _dbcontext.Replies.Where(x => x.UserId == id).ExecuteDeleteAsync();
            await _dbcontext.Threads.Where(x => x.UserId == id).ExecuteDeleteAsync();
            await _dbcontext.ThongBaos.Where(x => x.SenderId == id).ExecuteDeleteAsync();

            // Cascade tự xóa phần còn lại
            _dbcontext.Users.Remove(user);
            await _dbcontext.SaveChangesAsync();

            var identityUser = await _userManager.FindByIdAsync(id.ToString());
            if (identityUser != null)
                await _userManager.DeleteAsync(identityUser);

            return RedirectToAction("Index");
        }

        // ==================== SET STATUS ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(Guid id, byte status)
        {
            var user = await _dbcontext.Users.FindAsync(id);
            if (user == null)
                return Json(new { success = false, message = "Không tìm thấy user." });

            user.Status = status;
            user.UpdatedAt = DateTime.UtcNow;
            await _dbcontext.SaveChangesAsync();

            var msg = status switch
            {
                1 => "Đã mở khóa tài khoản.",
                2 => "Đã tạm khóa tài khoản.",
                3 => "Đã cấm tài khoản vĩnh viễn.",
                _ => "Cập nhật thành công."
            };
            return Json(new { success = true, message = msg });
        }

        // ==================== ASSIGN ROLE (inline — dùng cho dropdown trên bảng Index) ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(Guid userId, Guid roleId)
        {
            var user = await _dbcontext.Users.FindAsync(userId);
            if (user == null)
                return Json(new { success = false, message = "Không tìm thấy user." });

            var customRole = await _dbcontext.Roles.FindAsync(roleId);
            if (customRole == null)
                return Json(new { success = false, message = "Quyền không tồn tại." });

            // Cập nhật FK trong UsersModel
            user.RoleId = customRole.Id;
            user.UpdatedAt = DateTime.UtcNow;
            await _dbcontext.SaveChangesAsync();

            // Sync IdentityRole
            var identityUser = await _userManager.FindByIdAsync(userId.ToString());
            if (identityUser != null)
            {
                var currentRoles = await _userManager.GetRolesAsync(identityUser);
                await _userManager.RemoveFromRolesAsync(identityUser, currentRoles);
                if (await _roleManager.RoleExistsAsync(customRole.Name))
                    await _userManager.AddToRoleAsync(identityUser, customRole.Name);
            }

            return Json(new { success = true, message = $"Đã gán quyền {customRole.Name}." });
        }
    }

    // ==================== DTOs ====================
    public record CreateUserDto(
        string Username,
        string Email,
        string Password,
        string? DisplayName,
        Guid? RoleId       // FK → RolesModel
    );

    public record EditUserDto(
        string? DisplayName,
        Guid? RoleId       // FK → RolesModel
    );
}