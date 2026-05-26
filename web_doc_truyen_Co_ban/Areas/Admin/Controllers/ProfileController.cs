using DocumentFormat.OpenXml.VariantTypes;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using web_doc_truyen_Co_ban.Data;

namespace web_doc_truyen_Co_ban.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;
        private readonly IWebHostEnvironment _env;
        public ProfileController(ApplicationDbContext dbcontext ,IWebHostEnvironment env)
        {
            _dbcontext = dbcontext;
            _env = env;

        }
        //lấy id user hiên tại đăng nhâp hớ hớ fuckkkk
        private Guid? GetCurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw,out var id)?id:null ; 
        }
        public async Task< IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login","Account");
            }
            var user = await _dbcontext.Users.FindAsync(userId.Value);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
            
        }
        [HttpPost]
        public async Task<IActionResult> UpdateInfo(string? displayName, string? bio)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var user = await _dbcontext.Users.FindAsync(userId.Value);
            if (user == null)
                return NotFound();

            user.DisplayName = displayName;
            user.Bio = bio;
            user.UpdatedAt = DateTime.UtcNow;

            await _dbcontext.SaveChangesAsync();
            HttpContext.Session.SetString("Admin_DisplayName", user.DisplayName ?? user.Username);
            TempData["Success"] = "Cập nhật thông tin thành công.";
            return RedirectToAction(nameof(Index));
        }
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
            HttpContext.Session.SetString("Admin_AvatarUrl", user.AvatarUrl);

            TempData["Success"] = "Cập nhật ảnh đại diện thành công!";
            return RedirectToAction("Index");
        }
    }
}
