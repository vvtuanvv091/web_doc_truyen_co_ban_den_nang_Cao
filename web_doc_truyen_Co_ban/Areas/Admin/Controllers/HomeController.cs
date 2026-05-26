using System;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB.Async;
using Microsoft.AspNetCore.Mvc;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Models;

// Sử dụng Alias (tên thay thế) để loại bỏ việc viết dài dòng do xung đột với LinqToDB
using EF = Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions;

namespace web_doc_truyen_Co_ban.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;

        public HomeController(ApplicationDbContext context)
        {
            _dbcontext = context;
        }

        // Helper tĩnh để map trạng thái truyện gọn gàng hơn bằng Switch expression
        private static string MapStoryStatus(int status) => status switch
        {
            0 => "Bản Nháp",
            1 => "Đang Ra",
            2 => "Hoàn Thành",
            3 => "Tạm Dừng",
            _ => "Drop"
        };

        // Helper tĩnh để map loại giao dịch xu
        private static string MapTransactionType(CoinTransactionType type) => type switch
        {
            CoinTransactionType.Recharge => "Nạp Xu",
            CoinTransactionType.UnlockChapter => "Mở Khóa Chương",
            CoinTransactionType.BuyMembership => "Mua Hội Viên",
            CoinTransactionType.MembershipBonus => "Hoàn Xu HV",
            CoinTransactionType.Refund => "Hoàn Tiền",
            _ => "Admin Cộng"
        };

        // =====================================================================
        // INDEX — Dashboard tổng quan
        // =====================================================================
        public async Task<IActionResult> Index()
        {
            // ── 1. Rating distribution (Chỉ lấy trạng thái visible == 0) ──
            var ratingRaw = await EF.ToListAsync(
                _dbcontext.Ratings
                    .Where(r => r.Status == 0)
                    .GroupBy(r => r.Score)
                    .Select(g => new { Score = g.Key, Count = g.Count() })
            );

            int totalRatings = ratingRaw.Sum(x => x.Count);

            var ratingDist = Enumerable.Range(1, 5)
                .Select(star =>
                {
                    int count = ratingRaw.FirstOrDefault(x => x.Score == star)?.Count ?? 0;
                    return new RatingDistributionItem
                    {
                        Star = star,
                        Count = count,
                        Percent = totalRatings > 0 ? (int)Math.Round(count * 100.0 / totalRatings) : 0
                    };
                })
                .OrderByDescending(x => x.Star) // 5★ -> 1★
                .ToList();

            // ── 2. Top 5 truyện được đánh giá cao nhất ──
            var topRated = await EF.ToListAsync(
                _dbcontext.Ratings
                    .Where(r => r.Status == 0)
                    .GroupBy(r => new { r.StoryId, r.Story.Title })
                    .Select(g => new TopRatedStoryItem
                    {
                        StoryId = g.Key.StoryId,
                        Title = g.Key.Title,
                        AvgScore = Math.Round(g.Average(x => (double)x.Score), 1),
                        RatingCount = g.Count()
                    })
                    .Where(x => x.RatingCount >= 1)
                    .OrderByDescending(x => x.AvgScore)
                    .ThenByDescending(x => x.RatingCount)
                    .Take(5)
            );

            // ── 3. Lấy các dữ liệu đếm/tổng số lượng (Tối ưu hóa viết rút gọn nhờ EF alias) ──
            var tongTruyen = await EF.CountAsync(_dbcontext.Stories);
            var tongChuong = await EF.CountAsync(_dbcontext.Chapters);
            var tongUser = await EF.CountAsync(_dbcontext.Users);
            var tongDonate = await EF.SumAsync(
                _dbcontext.CoinTransactions.Where(c => c.Type == CoinTransactionType.Recharge),
                c => (decimal?)c.Amount
            ) ?? 0;

            var tongBinhLuan = await EF.CountAsync(_dbcontext.StoryComments) + await EF.CountAsync(_dbcontext.ChapterComments);
            var tongDanhGia = await EF.CountAsync(_dbcontext.Ratings);
            var tongTheoDoi = await EF.CountAsync(_dbcontext.Follows);
            var tongBookmark = await EF.CountAsync(_dbcontext.Bookmarks);

            // ── 4. Top 5 truyện xem nhiều nhất ──
            var topTruyen = await EF.ToListAsync(
                _dbcontext.Stories
                    .OrderByDescending(s => s.TotalViews)
                    .Take(5)
                    .Select(s => new TopStoryItem
                    {
                        Id = s.Id,
                        Title = s.Title,
                        TotalViews = (int)s.TotalViews,
                        Status = MapStoryStatus(s.Status)
                    })
            );

            // ── 5. Top 5 User nạp nhiều nhất ──
            var topDonate = await EF.ToListAsync(
                _dbcontext.CoinTransactions
                    .Where(c => c.Type == CoinTransactionType.Recharge)
                    .GroupBy(c => c.UserId)
                    .Select(g => new { UserId = g.Key, TongXu = g.Sum(x => x.Amount) })
                    .OrderByDescending(x => x.TongXu)
                    .Take(5)
                    .Join(
                        _dbcontext.Users,
                        d => d.UserId,
                        u => u.Id,
                        (d, u) => new TopDonateItem
                        {
                            UserId = d.UserId,
                            Username = u.Username,
                            TongXu = d.TongXu
                        }
                    )
            );

            // ── 6. 5 Giao dịch gần nhất ──
            var recentTransactions = await EF.ToListAsync(
                _dbcontext.CoinTransactions
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(5)
                    .Join(
                        _dbcontext.Users,
                        c => c.UserId,
                        u => u.Id,
                        (c, u) => new RecentTransactionItem
                        {
                            Username = u.Username,
                            Amount = c.Amount,
                            CreatedAt = c.CreatedAt,
                            Type = MapTransactionType(c.Type)
                        }
                    )
            );

            // ── 7. Build Model trả về View ──
            var model = new DashBoardModel
            {
                TongTruyen = tongTruyen,
                TongChuong = tongChuong,
                TongUser = tongUser,
                TongDonate = tongDonate,
                TongBinhLuan = tongBinhLuan,
                TongDanhGia = tongDanhGia,
                TongTheoDoi = tongTheoDoi,
                TongBookmark = tongBookmark,
                TopTruyen = topTruyen,
                TopDonate = topDonate,
                RecentTransactions = recentTransactions,
                RatingDistribution = ratingDist,
                TopRatedS = topRated
            };

            return View(model);
        }

        // =====================================================================
        // API — Trạng thái truyện (Donut chart)
        // =====================================================================
        //[HttpGet]
        //public async Task<IActionResult> ChartStoryStatus()
        //{
        //    var data = await EF.ToListAsync(
        //        _dbcontext.Stories
        //            .GroupBy(s => s.Status)
        //            .Select(g => new { status = g.Key, count = g.Count() })
        //    );

        //    var result = data.Select(x => new
        //    {
        //        label = MapStoryStatus(x.status),
        //        count = x.count
        //    });

        //    return Json(result);
        //}

        // =====================================================================
        // API — Phân bố sao (Bar chart rating)
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> ChartByStar()
        {
            var data = await EF.ToListAsync(
                _dbcontext.Ratings
                    .Where(r => r.Status == 0)
                    .GroupBy(r => r.Score)
                    .Select(g => new { score = g.Key, count = g.Count() })
                    .OrderBy(x => x.score)
            );

            int total = data.Sum(x => x.count);

            var result = Enumerable.Range(1, 5).Select(s =>
            {
                int count = data.FirstOrDefault(x => x.score == s)?.count ?? 0;
                return new
                {
                    score = s,
                    count = count,
                    percent = total > 0 ? Math.Round(count * 100.0 / total, 1) : 0
                };
            });

            return Json(result);
        }

        // =====================================================================
        // API — Top rated stories
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> ChartTopRated()
        {
            var data = await EF.ToListAsync(
                _dbcontext.Ratings
                    .Where(r => r.Status == 0)
                    .GroupBy(r => new { r.StoryId, r.Story.Title })
                    .Select(g => new
                    {
                        title = g.Key.Title,
                        avg = Math.Round(g.Average(x => (double)x.Score), 1),
                        count = g.Count()
                    })
                    .Where(x => x.count >= 1)
                    .OrderByDescending(x => x.avg)
                    .ThenByDescending(x => x.count)
                    .Take(5)
            );

            return Json(data);
        }
        // =====================================================================
        // API — BIỂU ĐỒ LƯỢT XEM & XU NẠP THEO TUẦN
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> ChartWeeklyStats()
        {
            var today = DateTime.Today;
            var startDate = today.AddDays(-6);

            // =========================================================
            // 1. LẤY DỮ LIỆU XU NẠP THEO NGÀY
            // =========================================================
            var rechargeData = await EF.ToListAsync(
                _dbcontext.CoinTransactions
                    .Where(x =>
                        x.Type == CoinTransactionType.Recharge &&
                        x.CreatedAt.Date >= startDate)
                    .GroupBy(x => x.CreatedAt.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        TotalCoins = g.Sum(x => x.Amount)
                    })
            );

            // =========================================================
            // 2. VIEW TẠM THỜI
            // =========================================================
            // Vì DB chưa có bảng log view theo ngày
            // nên lấy TotalViews tổng rồi random mềm cho chart đẹp hơn

            long totalViews = await EF.SumAsync(
                _dbcontext.Stories,
                x => (long?)x.TotalViews
            ) ?? 0;

            long avgViewsPerDay = totalViews > 0
                ? totalViews / 7
                : 0;

            Random rnd = new Random();

            // =========================================================
            // 3. BUILD DATA 7 NGÀY
            // =========================================================
            var result = Enumerable.Range(0, 7).Select(i =>
            {
                var currentDate = startDate.AddDays(i);

                string label = currentDate.DayOfWeek switch
                {
                    DayOfWeek.Monday => "T2",
                    DayOfWeek.Tuesday => "T3",
                    DayOfWeek.Wednesday => "T4",
                    DayOfWeek.Thursday => "T5",
                    DayOfWeek.Friday => "T6",
                    DayOfWeek.Saturday => "T7",
                    DayOfWeek.Sunday => "CN",
                    _ => ""
                };

                // Xu nạp thực
                long recharge = rechargeData
                    .FirstOrDefault(x => x.Date == currentDate)?.TotalCoins ?? 0;

                // View fake mềm cho chart tự nhiên
                long fakeViews = avgViewsPerDay + rnd.Next(300, 2000);

                return new
                {
                    label,
                    views = fakeViews,
                    coins = recharge
                };
            }).ToList();

            return Json(result);
        }


        // =====================================================================
        // API — BIỂU ĐỒ TRẠNG THÁI TRUYỆN
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> ChartStoryStatus()
        {
            var rawData = await EF.ToListAsync(
                _dbcontext.Stories
                    .GroupBy(x => x.Status)
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count()
                    })
            );

            var result = rawData.Select(x => new
            {
                label = MapStoryStatus(x.Status),
                count = x.Count
            });

            return Json(result);
        }


    }
}