using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Models;

public class UnlockChapterController : Controller
{
    private readonly ApplicationDbContext _dbcontext;

    public UnlockChapterController(ApplicationDbContext context)
    {
        _dbcontext = context;
    }

    // GET: /UnlockChapter/Confirm?id=...

    // Helper: unlock miễn phí
    // Helper dùng chung trong controller
    private static DateTime GetVietnamToday()
    {
        var vnZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); // UTC+7
        var vnNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnZone);
        return vnNow.Date; // 00:00:00 hôm nay theo giờ VN
    }

    public async Task<IActionResult> Confirm(Guid id)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr))
            return RedirectToAction("Login", "Account");

        var chapter = await _dbcontext.Chapters
            .Include(c => c.Story)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (chapter == null) return NotFound();

        if (!(chapter.IsLocked || chapter.Status == 2))
            return RedirectToAction("Index", "Chapter", new { id });

        var userId = Guid.Parse(userIdStr);

        // Đã unlock rồi → đọc luôn không hỏi
        var alreadyUnlocked = await _dbcontext.UnlockedChapters
            .AnyAsync(x => x.UserId == userId && x.ChapterId == id);
        if (alreadyUnlocked)
            return RedirectToAction("Index", "Chapter", new { id });

        var user = await _dbcontext.Users.FirstOrDefaultAsync(x => x.Id == userId);

        // Kiểm tra membership
        var membership = await _dbcontext.UserMemberships
            .Include(x => x.Plan)
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.StartDate <= DateTime.UtcNow &&
                x.EndDate >= DateTime.UtcNow);

        // Tính lượt free còn lại hôm nay
        int freeLeft = 0;
        if (membership?.Plan != null)
        {
            var plan = membership.Plan;
            if (plan.ChapterUnlockPerDay == -1)
            {
                freeLeft = 999; // Kim Cương: không giới hạn
            }
            else if (plan.ChapterUnlockPerDay > 0)
            {
                var todayVn = GetVietnamToday();
                var tomorrowVn = todayVn.AddDays(1);

                // Đổi ngày VN sang UTC để so sánh với UnlockedAt (lưu UTC)
                var vnZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var todayUtc = TimeZoneInfo.ConvertTimeToUtc(todayVn, vnZone);
                var tomorrowUtc = TimeZoneInfo.ConvertTimeToUtc(tomorrowVn, vnZone);

                var unlockedToday = await _dbcontext.UnlockedChapters
                    .CountAsync(x =>
                        x.UserId == userId &&
                        x.UnlockedAt >= todayUtc &&
                        x.UnlockedAt < tomorrowUtc);

                freeLeft = Math.Max(0, plan.ChapterUnlockPerDay - unlockedToday);
            }
        }

        ViewBag.UserCoins = user?.coins ?? 0;
        ViewBag.FreeLeft = freeLeft;
        ViewBag.MembershipName = membership?.Plan?.Name ?? "";
        return View(chapter);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DoUnlock(Guid chapterId, bool useFree = false)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr))
            return Json(new { success = false, message = "Vui lòng đăng nhập." });

        var userId = Guid.Parse(userIdStr);

        var chapter = await _dbcontext.Chapters
            .Include(c => c.Story)
            .FirstOrDefaultAsync(c => c.Id == chapterId);
        if (chapter == null)
            return Json(new { success = false, message = "Không tìm thấy chương." });

        var alreadyUnlocked = await _dbcontext.UnlockedChapters
            .AnyAsync(x => x.UserId == userId && x.ChapterId == chapterId);
        if (alreadyUnlocked)
            return Json(new { success = true, redirectUrl = Url.Action("Index", "Chapter", new { id = chapterId }) });

        var user = await _dbcontext.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null)
            return Json(new { success = false, message = "Không tìm thấy người dùng." });

        if (useFree)
        {
            // Dùng lượt free → kiểm tra lại còn lượt không
            var membership = await _dbcontext.UserMemberships
                .Include(x => x.Plan)
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.StartDate <= DateTime.UtcNow &&
                    x.EndDate >= DateTime.UtcNow);

            if (membership?.Plan == null)
                return Json(new { success = false, message = "Không có gói hội viên." });

            var plan = membership.Plan;

            if (plan.ChapterUnlockPerDay != -1 && plan.ChapterUnlockPerDay > 0)
            {
                var todayVn = GetVietnamToday();
                var tomorrowVn = todayVn.AddDays(1);
                var vnZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                var todayUtc = TimeZoneInfo.ConvertTimeToUtc(todayVn, vnZone);
                var tomorrowUtc = TimeZoneInfo.ConvertTimeToUtc(tomorrowVn, vnZone);

                var unlockedToday = await _dbcontext.UnlockedChapters
                    .CountAsync(x =>
                        x.UserId == userId &&
                        x.UnlockedAt >= todayUtc &&
                        x.UnlockedAt < tomorrowUtc);

                if (unlockedToday >= plan.ChapterUnlockPerDay)
                    return Json(new { success = false, message = "Hết lượt miễn phí hôm nay. Reset lúc 00:00 ngày mai." });
            }

            // Unlock free
            _dbcontext.UnlockedChapters.Add(new UnlockedChapter
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ChapterId = chapterId,
                UnlockedAt = DateTime.UtcNow
            });

            await _dbcontext.SaveChangesAsync();
            return Json(new { success = true, redirectUrl = Url.Action("Index", "Chapter", new { id = chapterId }) });
        }
        else
        {
            // Dùng xu
            if (user.coins < chapter.UnlockPrice)
                return Json(new
                {
                    success = false,
                    notEnough = true,
                    message = $"Không đủ xu. Cần {chapter.UnlockPrice} xu, bạn có {user.coins} xu."
                });

            user.coins -= chapter.UnlockPrice;
            user.UpdatedAt = DateTime.UtcNow;

            _dbcontext.CoinTransactions.Add(new CoinTransaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Amount = -chapter.UnlockPrice,
                BalanceAfter = user.coins,
                Type = CoinTransactionType.UnlockChapter,
                Note = $"Mở khóa chương {chapter.ChapterNumber} - {chapter.Story?.Title}",
                CreatedAt = DateTime.UtcNow
            });

            _dbcontext.UnlockedChapters.Add(new UnlockedChapter
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ChapterId = chapterId,
                UnlockedAt = DateTime.UtcNow
            });

            await _dbcontext.SaveChangesAsync();
            return Json(new { success = true, coinsLeft = user.coins, redirectUrl = Url.Action("Index", "Chapter", new { id = chapterId }) });
        }
    }
}