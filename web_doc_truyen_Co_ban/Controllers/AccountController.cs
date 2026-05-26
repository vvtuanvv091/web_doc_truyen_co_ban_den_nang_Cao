using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.DTO;
using web_doc_truyen_Co_ban.Hubs;
using web_doc_truyen_Co_ban.Models;
using web_doc_truyen_Co_ban.Services;

namespace web_doc_truyen_Co_ban.Controllers
{

    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ErrorAdminViewService _error;
        private readonly IHubContext<NotificationHub> _hub;
        public AccountController(
            ApplicationDbContext db,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ErrorAdminViewService errorDb,
            IHubContext<NotificationHub>hub)
        {
            _dbcontext = db;
            _userManager = userManager;
            _signInManager = signInManager;
            _error = errorDb;
            _hub= hub;
            
        }

        // ==================== REGISTER ====================
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            return View();
        }

        //code AI
        // ==================== GOOGLE LOGIN ====================
        [HttpGet]
        public IActionResult LoginWithGoogle()
        {
            var redirectUrl = Url.Action("GoogleCallback", "Account");
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
            return Challenge(properties, "Google");
        }

        [HttpGet]
        public async Task<IActionResult> GoogleCallback()
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return RedirectToAction("Login");

            // Đã liên kết Google trước đó → đăng nhập thẳng
            var result = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider, info.ProviderKey, isPersistent: false);

            if (result.Succeeded)
            {
                var existingIdentity = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                var customUser = await _dbcontext.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == Guid.Parse(existingIdentity!.Id));

                if (customUser != null)
                {
                    customUser.LastLoginAt = DateTime.UtcNow;
                    await _dbcontext.SaveChangesAsync();
                    await CreateSessionAsync(customUser.Id, rememberMe: false);
                    SetHttpSession(customUser);
                }
                return RedirectToAction("Index", "Home");
            }

            // Lần đầu đăng nhập Google → lấy thông tin
            var email = info.Principal.FindFirstValue(System.Security.Claims.ClaimTypes.Email);
            var name = info.Principal.FindFirstValue(System.Security.Claims.ClaimTypes.Name) ?? email;

            if (email == null)
                return RedirectToAction("Login");

            // Kiểm tra email đã tồn tại chưa
            var identityUser = await _userManager.FindByEmailAsync(email);
            if (identityUser == null)
            {
                identityUser = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };
                await _userManager.CreateAsync(identityUser);
            }

            // Liên kết Google với tài khoản
            await _userManager.AddLoginAsync(identityUser, info);

            // Tạo bản ghi trong bảng Users nếu chưa có
            var exists = await _dbcontext.Users.AnyAsync(u => u.Id == Guid.Parse(identityUser.Id));
            if (!exists)
            {
                var defaultRole = await _dbcontext.Roles
                    .FirstOrDefaultAsync(r => r.Name == "Người Dùng");

                var newUser = new UsersModel
                {
                    Id = Guid.Parse(identityUser.Id),
                    Username = email,
                    Email = email,
                    PasswordHash = "",
                    DisplayName = name ?? email,
                    Status = 1,
                    EmailVerified = true,
                    RoleId = defaultRole?.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                if (defaultRole != null)
                    await _userManager.AddToRoleAsync(identityUser, defaultRole.Name);

                _dbcontext.Users.Add(newUser);
                await _dbcontext.SaveChangesAsync();
            }

            // Đăng nhập và set session
            await _signInManager.SignInAsync(identityUser, isPersistent: false);

            var user = await _dbcontext.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == Guid.Parse(identityUser.Id));

            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _dbcontext.SaveChangesAsync();
                await CreateSessionAsync(user.Id, rememberMe: false);
                SetHttpSession(user);
            }

            // Thông báo cho admin
            var danhSachAdminId = await _dbcontext.Users
                .Where(u => u.Role != null && u.Role.Name == "Quản Trị Viên")
                .Select(u => u.Id)
                .ToListAsync();

            if (danhSachAdminId.Any())
            {
                var link = Url.Action("Edit", "User", new { area = "Admin", id = user?.Id });
                var danhSachTb = danhSachAdminId.Select(uid => new ThongBaoModel
                {
                    UserId = uid,
                    Type = "login",
                    Message = $"Người dùng \"{user?.DisplayName}\" vừa đăng nhập bằng Google",
                    Link = link
                }).ToList();

                _dbcontext.ThongBaos.AddRange(danhSachTb);
                await _dbcontext.SaveChangesAsync();

                var soThongBaoMoi = await _dbcontext.ThongBaos
                    .Where(t => danhSachAdminId.Contains(t.UserId) && !t.IsRead)
                    .GroupBy(t => t.UserId)
                    .Select(g => new { UserId = g.Key, SoMoi = g.Count() })
                    .ToDictionaryAsync(x => x.UserId, x => x.SoMoi);

                var tasks = danhSachAdminId.Select(async uid =>
                {
                    var soMoi = soThongBaoMoi.TryGetValue(uid, out var count) ? count : 0;
                    await _hub.Clients
                        .Group($"user_{uid}")
                        .SendAsync("NhanThongBaoMoi", new
                        {
                            type = "login",
                            message = $"Người dùng \"{user?.DisplayName}\" vừa đăng nhập bằng Google",
                            link,
                            time = DateTime.Now.ToString("HH:mm dd/MM/yyyy"),
                            soMoi
                        });
                });
                await Task.WhenAll(tasks);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto dto,UsersModel adminId)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || dto.Username.Length < 3)
                return Json(new { success = false, message = "Tên đăng nhập phải có ít nhất 3 ký tự." });

            if (string.IsNullOrWhiteSpace(dto.Email))
                return Json(new { success = false, message = "Email không hợp lệ." });

            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
                return Json(new { success = false, message = "Mật khẩu phải có ít nhất 6 ký tự." });

            if (dto.Password != dto.ConfirmPassword)
                return Json(new { success = false, message = "Mật khẩu xác nhận không khớp." });

            bool usernameExists = await _dbcontext.Users
                .AnyAsync(u => u.Username.ToLower() == dto.Username.Trim().ToLower());
            if (usernameExists)
            {
                await _error.LogErrorAsync(
                    null,
                    "Register",
                    "User",
                    null,
                    "Username already exists"
                );
                return Json(new { success = false, message = "Tên đăng nhập đã tồn tại." });
            }

            // --- Tạo IdentityUser (ghi vào AspNetUsers) ---
            var identityUser = new IdentityUser
            {
                UserName = dto.Email.Trim().ToLower(),
                Email = dto.Email.Trim().ToLower(),
                EmailConfirmed = false
            };

            var createResult = await _userManager.CreateAsync(identityUser, dto.Password);
            if (!createResult.Succeeded)
            {
                var errorMsg = createResult.Errors.FirstOrDefault()?.Description ?? "Đăng ký thất bại.";
                if (errorMsg.Contains("already taken") || errorMsg.Contains("is already"))
                    errorMsg = "Email này đã được sử dụng.";

                return Json(new { success = false, message = errorMsg });
            }

            // --- Ghi vào dbo.Users (dùng cùng Guid Id với AspNetUsers) ---
            var customUser = new UsersModel
            {
                Id = Guid.Parse(identityUser.Id),
                Username = dto.Username.Trim(),
                Email = dto.Email.Trim().ToLower(),
                PasswordHash = identityUser.PasswordHash!,
                DisplayName = dto.Username.Trim(),
                Status = 1,
                EmailVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var defaultRole = await _dbcontext.Roles
                .FirstOrDefaultAsync(r => r.Name == "Người Dùng");
            if (defaultRole != null)
            {
                customUser.RoleId = defaultRole.Id;
                // Sync IdentityRole
                await _userManager.AddToRoleAsync(identityUser, defaultRole.Name);
            }

            _dbcontext.Users.Add(customUser);
            await _dbcontext.SaveChangesAsync();


            //// --- Đăng nhập ngay ---
            await _signInManager.SignInAsync(identityUser, isPersistent: false);

            // --- Ghi vào dbo.UsersSessions ---
            await CreateSessionAsync(customUser.Id, rememberMe: false);

            SetHttpSession(customUser);
            await _error.LogAsync(
                customUser.Id,
                "Register",
                "User",
                customUser.Id
            );

            // Lấy TẤT CẢ admin
            var danhSachAdminId = await _dbcontext.Users
                .Where(u => u.Role != null && u.Role.Name == "Quản Trị Viên")
                .Select(u => u.Id)
                .ToListAsync();

            if (danhSachAdminId.Any())
            {
                var link = Url.Action("Edit", "User", new { area = "Admin", id = customUser.Id });

                var danhSachTb = danhSachAdminId.Select(uid => new ThongBaoModel
                {
                    UserId = uid,
                    Type = "new_user",   // ✅ khớp với iconMap
                    Message = $"Người dùng \"{customUser.DisplayName}\" vừa đăng ký tài khoản",
                    Link = link
                }).ToList();

                _dbcontext.ThongBaos.AddRange(danhSachTb);
                await _dbcontext.SaveChangesAsync();

                var soThongBaoMoi = await _dbcontext.ThongBaos
                    .Where(t => danhSachAdminId.Contains(t.UserId) && !t.IsRead)
                    .GroupBy(t => t.UserId)
                    .Select(g => new { UserId = g.Key, SoMoi = g.Count() })
                    .ToDictionaryAsync(x => x.UserId, x => x.SoMoi);

                var tasks = danhSachAdminId.Select(async uid =>
                {
                    var soMoi = soThongBaoMoi.TryGetValue(uid, out var count) ? count : 0;

                    await _hub.Clients
                        .Group($"user_{uid}")
                        .SendAsync("NhanThongBaoMoi", new
                        {
                            type = "new_user",   // ✅ đồng nhất
                            message = $"Người dùng \"{customUser.DisplayName}\" vừa đăng ký tài khoản",
                            link,
                            time = DateTime.Now.ToString("HH:mm dd/MM/yyyy"),
                            soMoi
                        });
                });

                await Task.WhenAll(tasks);
            }

            return Json(new
            {
                success = true,
                message = "Đăng ký thành công!",
                redirectUrl = Url.Action("Login", "Account")
            });
        }

        // ==================== LOGIN ====================
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var identityUser = await _userManager.FindByEmailAsync(dto.Email);
            if (identityUser == null)
                return Json(new { success = false, message = "Email hoặc mật khẩu không đúng." });

            var customUser = await _dbcontext.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == Guid.Parse(identityUser.Id));

            if (customUser != null)
            {
                if (customUser.Status == 2)
                    return Json(new { success = false, message = "Tài khoản đang bị tạm khóa." });
                if (customUser.Status == 3)
                    return Json(new { success = false, message = "Tài khoản đã bị cấm vĩnh viễn." });
            }

            var result = await _signInManager.PasswordSignInAsync(
                identityUser, dto.Password,
                isPersistent: dto.RememberMe,
                lockoutOnFailure: false);

            if (!result.Succeeded)
                return Json(new { success = false, message = "Email hoặc mật khẩu không đúng." });

            if (customUser != null)
            {
                customUser.LastLoginAt = DateTime.UtcNow;
                await _dbcontext.SaveChangesAsync();

                var session = await CreateSessionAsync(customUser.Id, dto.RememberMe);

                if (dto.RememberMe && session != null)
                {
                    Response.Cookies.Append("RefreshToken", session.RefreshToken, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        Expires = DateTimeOffset.UtcNow.AddDays(30),
                        SameSite = SameSiteMode.Strict
                    });
                }

                SetHttpSession(customUser);
            }
            var roleName = customUser?.Role?.Name ?? "";
            var redirectUrl = roleName switch
            {
                "Quản Trị Viên" => Url.Action("Index", "Home", new { area = "Admin" }),
                "Hỗ Trợ Viên" => Url.Action("Index", "Home", new { area = "Admin" }),
                "Người Đăng Truyện" => Url.Action("Index", "Home", new { area = "Admin" }),
                _ => Url.Action("Index", "Home") // User thường
            };
            // Lấy TẤT CẢ admin
            var danhSachAdminId = await _dbcontext.Users
                .Where(u => u.Role != null && u.Role.Name == "Quản Trị Viên")
                .Select(u => u.Id)
                .ToListAsync();

            if (danhSachAdminId.Any())
            {
                var link = Url.Action("Edit", "User", new { area = "Admin", id = customUser.Id });

                var danhSachTb = danhSachAdminId.Select(uid => new ThongBaoModel
                {
                    UserId = uid,
                    Type = "login ",   // ✅ khớp với iconMap
                    Message = $"Người dùng \"{customUser.DisplayName}\" vừa đăng nhập tài khoản vào web.... ",
                    Link = link
                }).ToList();

                _dbcontext.ThongBaos.AddRange(danhSachTb);
                await _dbcontext.SaveChangesAsync();

                var soThongBaoMoi = await _dbcontext.ThongBaos
                    .Where(t => danhSachAdminId.Contains(t.UserId) && !t.IsRead)
                    .GroupBy(t => t.UserId)
                    .Select(g => new { UserId = g.Key, SoMoi = g.Count() })
                    .ToDictionaryAsync(x => x.UserId, x => x.SoMoi);

                var tasks = danhSachAdminId.Select(async uid =>
                {
                    var soMoi = soThongBaoMoi.TryGetValue(uid, out var count) ? count : 0;

                    await _hub.Clients
                        .Group($"user_{uid}")
                        .SendAsync("NhanThongBaoMoi", new
                        {
                            type = "login",   // ✅ đồng nhất
                            message = $"Người dùng \"{customUser.DisplayName}\" vừa đăng nhập tài khoản vào web.... ",
                            link,
                            time = DateTime.Now.ToString("HH:mm dd/MM/yyyy"),
                            soMoi
                        });
                });

                await Task.WhenAll(tasks);
            }
            return Json(new { success = true, redirectUrl });
        }

        // ==================== LOGOUT ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            Response.Cookies.Delete("RefreshToken");
            return RedirectToAction("Login");
        }

        // ==================== HELPERS ====================
        private async Task<UsersSessionModel?> CreateSessionAsync(Guid userId, bool rememberMe)
        {
            var session = new UsersSessionModel
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RefreshToken = Guid.NewGuid().ToString("N"),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                ExpiresAt = rememberMe
                                 ? DateTime.UtcNow.AddDays(30)
                                 : DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow
            };
            _dbcontext.UsersSessions.Add(session);
            await _dbcontext.SaveChangesAsync();
            return session;
        }

        private void SetHttpSession(UsersModel user)
        {
            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("DisplayName", user.DisplayName ?? user.Username);
            HttpContext.Session.SetString("AvatarUrl", user.AvatarUrl ?? "");
            if (user.Role != null && user.Role.Name == "Quản Trị Viên")
            {
                HttpContext.Session.SetString("Admin_Username", user.Username);
                HttpContext.Session.SetString("Admin_DisplayName", user.DisplayName ?? user.Username);
                HttpContext.Session.SetString("Admin_AvatarUrl", user.AvatarUrl ?? "");

            }    



        }
        [HttpGet]
        [Route("Account/Logout")]
        public async Task<IActionResult> LogoutGet()
        {
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            Response.Cookies.Delete("RefreshToken");
            return RedirectToAction("Login");
        }
    }
}