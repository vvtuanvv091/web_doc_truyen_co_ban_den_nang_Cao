using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Models;

namespace web_doc_truyen_Co_ban.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;
        private readonly IWebHostEnvironment _env;

        public ProfileController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _dbcontext = db;
            _env = env;
        }

        private Guid? GetCurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
        // GET /Profile
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var user = await _dbcontext.Users.FindAsync(userId.Value);
            if (user == null) return NotFound();
            //lấy gọi hốivie

            ViewBag.MemberShip= await _dbcontext.UserMemberships
                .Include(m => m.Plan)
                .Where(m => m.UserId == userId.Value && m.EndDate >= DateTime.UtcNow)
                .OrderByDescending(m => m.EndDate)
                .FirstOrDefaultAsync();

            // Theo dõi
            ViewBag.Follows = await _dbcontext.Follows
                .Include(f => f.Story).ThenInclude(s => s.Category)
                .Where(f => f.UserId == userId.Value)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            // Lịch sử đọc
            ViewBag.ReadingHistories = await _dbcontext.ReadingHistories
                .Include(h => h.Story)
                .Include(h => h.Chapter)
                .Where(h => h.UserId == userId.Value)
                .OrderByDescending(h => h.LastReadAt)
                .ToListAsync();
            //lchj sử trù xu
            ViewBag.Transactions = await _dbcontext.CoinTransactions
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(50)
                .ToListAsync();

            return View(user);
        }
        // POST /Profile/UpdateInfo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInfo(string displayName, string bio)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var user = await _dbcontext.Users.FindAsync(userId.Value);
            if (user == null) return NotFound();

            if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > 100)
            {
                TempData["Error"] = "Tên hiển thị không hợp lệ (tối đa 100 ký tự).";
                return RedirectToAction("Index");
            }

            user.DisplayName = displayName.Trim();
            user.Bio = bio?.Trim();
            user.UpdatedAt = DateTime.UtcNow;

            await _dbcontext.SaveChangesAsync();

            // Cập nhật session
            HttpContext.Session.SetString("DisplayName", user.DisplayName ?? user.Username);

            TempData["Success"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("Index");
        }
        // POST /Profile/UpdateAvatar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAvatar(IFormFile avatar)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var user = await _dbcontext.Users.FindAsync(userId.Value);
            if (user == null) return NotFound();

            if (avatar == null || avatar.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn ảnh.";
                return RedirectToAction("Index");
            }

            // Kiểm tra định dạng
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(avatar.FileName).ToLower();
            if (!allowed.Contains(ext))
            {
                TempData["Error"] = "Chỉ chấp nhận ảnh jpg, png, webp.";
                return RedirectToAction("Index");
            }

            // Kiểm tra dung lượng (30MB)
            if (avatar.Length > 30 * 1024 * 1024)
            {
                TempData["Error"] = "Ảnh không được vượt quá 30MB.";
                return RedirectToAction("Index");
            }
            if (!string.IsNullOrEmpty(user.AvatarUrl))
            {
                var oldFileName = Path.GetFileName(user.AvatarUrl);

                var oldPath = Path.Combine(
                    _env.WebRootPath,
                    "uploads",
                    "avatars",
                    oldFileName
                );

                if (System.IO.File.Exists(oldPath))
                {
                    System.IO.File.Delete(oldPath);
                }
            }
            // Lưu avatar mới
            var fileName = $"avatar_{Guid.NewGuid():N}{ext}";
            //var fileName = $"avatar_{userId}{ext}";
            var folder = Path.Combine(_env.WebRootPath, "uploads", "avatars");
            Directory.CreateDirectory(folder);
            var filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await avatar.CopyToAsync(stream);

            user.AvatarUrl = $"/uploads/avatars/{fileName}";
            user.UpdatedAt = DateTime.UtcNow;

            await _dbcontext.SaveChangesAsync();

            // Cập nhật session
            HttpContext.Session.SetString("AvatarUrl", user.AvatarUrl);

            TempData["Success"] = "Cập nhật ảnh đại diện thành công!";
            return RedirectToAction("Index");
        }
        // POST /Profile/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var user = await _dbcontext.Users.FindAsync(userId.Value);
            if (user == null) return NotFound();

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                TempData["Error"] = "Mật khẩu mới phải có ít nhất 6 ký tự.";
                return RedirectToAction("Index");
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Mật khẩu xác nhận không khớp.";
                return RedirectToAction("Index");
            }

            // Kiểm tra mật khẩu hiện tại bằng BCrypt
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            {
                TempData["Error"] = "Mật khẩu hiện tại không đúng.";
                return RedirectToAction("Index");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _dbcontext.SaveChangesAsync();

            TempData["Success"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Index");
        }
    }
}