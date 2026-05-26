using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client.Extensions.Msal;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Hubs;
using web_doc_truyen_Co_ban.Models;

namespace web_doc_truyen_Co_ban.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ChapterController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;
        private readonly IHubContext<NotificationHub> _hub;
        public ChapterController(ApplicationDbContext context, IHubContext<NotificationHub> hub)
        {
            _dbcontext = context;
                        _hub = hub;
        }
        //public async Task<IActionResult> Index(string? search, int page = 1)
        //{
        //    ViewData["ActiveMenu"] = "Chapter";
        //    int pageSize = 10;
        //    var query = _dbcontext.Chapters
        //        .Include(x => x.Story)
        //        .AsQueryable();
        //    if (!string.IsNullOrEmpty(search))
        //    {
        //        search = search.Trim().ToLower();
        //        query = query.Where(c => c.Title.ToLower().Contains(search)
        //                              || c.Story.Title.ToLower().Contains(search));
        //        ViewData["Search"] = search;
        //    }
        //    int total = await query.CountAsync();//đếm tổng
        //    var chapters = await query
        //        .OrderByDescending(x => x.CreatedAt)//ms nhất
        //        .Skip((page - 1) * pageSize)//bỏ qua các trang trc
        //        .Take(pageSize)//;ấy đúng page tại bản ghi
        //        .ToListAsync();//này ms thực sự chạy sql
        //    ViewBag.Page = page;
        //    ViewBag.PageSize = pageSize;
        //    ViewBag.Total = total;
        //    ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
        //    return View(chapters);
        //}



        public async Task<IActionResult> Index(string? search, Guid? storyId, int page = 1)
        {
            ViewData["ActiveMenu"] = "Chapter";
            int pageSize = 10;

            var query = _dbcontext.Chapters
                .Include(x => x.Story)
                .AsQueryable();

            // 🔍 Search text
            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(c => c.Title.ToLower().Contains(search)
                                      || c.Story.Title.ToLower().Contains(search));
                ViewData["Search"] = search;
            }

            // 🎯 Filter theo truyện
            if (storyId.HasValue)
            {
                query = query.Where(c => c.StoryId == storyId);
                ViewData["StoryId"] = storyId;
            }

            int total = await query.CountAsync();

            var chapters = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 🔽 Load dropdown truyện
            ViewBag.Stories = new SelectList(_dbcontext.Stories, "Id", "Title", storyId);

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

            return View(chapters);
        }
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Stories = new SelectList(_dbcontext.Stories.OrderBy(s => s.Title), "Id", "Title");
            return View();

        }
        //[HttpPost]
        //public async Task<IActionResult> Create(ChapterModel chapter)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        chapter.Id = Guid.NewGuid();
        //        chapter.CreatedAt = DateTime.Now;
        //        var story = await _dbcontext.Stories.FindAsync(chapter.StoryId);
        //        if (story != null) story.LastChapterAt = DateTime.Now;
        //        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //        _dbcontext.Chapters.Add(chapter);
        //        await _dbcontext.SaveChangesAsync();
        //        return RedirectToAction("Index");
        //    }
        //    return View(chapter);
        //}
        [HttpPost]
        public async Task<IActionResult> Create(ChapterModel chapter)
        {
            if (ModelState.IsValid)
            {
                chapter.Id = Guid.NewGuid();
                chapter.CreatedAt = DateTime.Now;

                if (!string.IsNullOrWhiteSpace(chapter.Content))
                    chapter.Content = chapter.Content.Normalize(NormalizationForm.FormC);

                var story = await _dbcontext.Stories.FindAsync(chapter.StoryId);
                if (story != null)
                    story.LastChapterAt = DateTime.Now;

                _dbcontext.Chapters.Add(chapter);
                await _dbcontext.SaveChangesAsync();

                // ── Push thông báo chương mới cho người theo dõi ──
                if (story != null)
                {
                    // Lấy danh sách userId đang follow truyện này
                    var nguoiTheoDoi = await _dbcontext.Follows
                        .Where(td => td.StoryId == chapter.StoryId)
                        .Select(td => td.UserId)
                        .ToListAsync();

                    if (nguoiTheoDoi.Any())
                    {
                        var linkChuong = $"/Chapter/Index/{chapter.Id}";

                        // Tạo thông báo hàng loạt
                        var danhSachTb = nguoiTheoDoi.Select(uid => new ThongBaoModel
                        {
                            UserId = uid,
                            Type = "new_chapter",
                            Message = $"Truyện \"{story.Title}\" vừa ra chương mới: {chapter.Title}",
                            Link = linkChuong
                        }).ToList();

                        _dbcontext.ThongBaos.AddRange(danhSachTb);
                        await _dbcontext.SaveChangesAsync();

                        // Push realtime song song
                        //var tasks = nguoiTheoDoi.Select(async uid =>
                        //{
                        //    var soMoi = await _dbcontext.ThongBaos
                        //        .CountAsync(t => t.UserId == uid && !t.IsRead);

                        //    await _hub.Clients
                        //        .Group($"user_{uid}")
                        //        .SendAsync("NhanThongBaoMoi", new
                        //        {
                        //            type = "new_chapter",
                        //            message = $"Truyện \"{story.Title}\" vừa ra chương mới: {chapter.Title}",
                        //            link = linkChuong,
                        //            time = DateTime.Now.ToString("HH:mm dd/MM/yyyy"),
                        //            soMoi
                        //        });
                        //});

                        //await Task.WhenAll(tasks);

                        // ✅ Chỉ push SignalR bên trong - KHÔNG đụng DbContext
                        var soThongBaoMoi = await _dbcontext.ThongBaos
                        .Where(t => nguoiTheoDoi.Contains(t.UserId) && !t.IsRead)
                        .GroupBy(t => t.UserId)
                        .Select(g => new { UserId = g.Key, SoMoi = g.Count() })
                        .ToDictionaryAsync(x => x.UserId, x => x.SoMoi);
                        var tasks = nguoiTheoDoi.Select(async uid =>
                        {
                            var soMoi = soThongBaoMoi.TryGetValue(uid, out var count) ? count : 0;

                            await _hub.Clients
                                .Group($"user_{uid}")
                                .SendAsync("NhanThongBaoMoi", new
                                {
                                    type = "new_chapter",
                                    message = $"Truyện \"{story.Title}\" vừa ra chương mới: {chapter.Title}",
                                    link = linkChuong,
                                    time = DateTime.Now.ToString("HH:mm dd/MM/yyyy"),
                                    soMoi
                                });
                        });

                        await Task.WhenAll(tasks);

                    }
                }

                return RedirectToAction("Index");
            }

            ViewBag.Stories = new SelectList(_dbcontext.Stories, "Id", "Title");
            return View(chapter);
        }
   
        [HttpGet]
        public IActionResult Edit(Guid id)
        {
            var chapter = _dbcontext.Chapters.Find(id);
            if (chapter == null)
            {
                return NotFound();

            }
            ViewBag.Stories = new SelectList(_dbcontext.Stories, "Id", "Title", chapter.StoryId);
            return View(chapter);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ChapterModel chapter)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Chapterss = new SelectList(_dbcontext.Stories, "Id", "Title", chapter.StoryId);
                return View(chapter);
            }
            var chapterIndb = _dbcontext.Chapters.Find(chapter.Id);
            if (chapterIndb == null)
            {
                return NotFound();
            }
            chapterIndb.StoryId = chapter.StoryId;

            chapterIndb.Title = chapter.Title; 
            chapterIndb.VolumeTitle = chapter.VolumeTitle;
            chapterIndb.ChapterNumber = chapter.ChapterNumber;
            chapterIndb.Content = chapter.Content;
            chapterIndb.WordCount = chapter.WordCount;
            chapterIndb.ChapterViews = chapter.ChapterViews;
            chapterIndb.ChapterComments = chapter.ChapterComments;
            chapterIndb.Status = chapter.Status;
            chapterIndb.IsLocked = chapter.IsLocked;
            chapterIndb.UnlockPrice = chapter.UnlockPrice;
            chapterIndb.PublishedAt = chapter.PublishedAt;
            chapterIndb.UpdatedAt = DateTime.Now;
            await _dbcontext.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> GetVolumes(Guid storyId)
        {
            var volumes = await _dbcontext.Chapters
                .Where(c => c.StoryId == storyId && c.VolumeTitle != null)
                .Select(c => c.VolumeTitle)
                .Distinct()
                .OrderBy(v => v)
                .ToListAsync();

            return Json(volumes);
        }
        public IActionResult Delete(Guid id)
        {
            var chapter = _dbcontext.Chapters.Find(id);
            if (chapter == null)
            {
                return NotFound();
            }
            return View(chapter);
        }
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var chapter = _dbcontext.Chapters.Find(id);
            if (chapter == null)
            {
                return NotFound();
            }
            // Xóa các bảng liên quan trước (vì dùng NoAction)
            //var follows = _dbcontext.Follows.Where(f => f.StoryId == id);
            //_dbcontext.Follows.RemoveRange(follows);

            //var bookmarks = _dbcontext.Bookmarks.Where(b => b.StoryId == id);
            //_dbcontext.Bookmarks.RemoveRange(bookmarks);

            //var ratings = _dbcontext.Ratings.Where(r => r.StoryId == id);
            //_dbcontext.Ratings.RemoveRange(ratings);

            var chapterViews = _dbcontext.ChapterViews.Where(cv => cv.ChapterId == id);
            _dbcontext.ChapterViews.RemoveRange(chapterViews);

            var readingHistories = _dbcontext.ReadingHistories.Where(rh => rh.ChapterId == id);
            _dbcontext.ReadingHistories.RemoveRange(readingHistories);

            //var chapterComments = _dbcontext.ChapterComments.Where(cc => cc.StoryId == id);
            //_dbcontext.ChapterComments.RemoveRange(chapterComments);
            _dbcontext.Chapters.Remove(chapter);
            await _dbcontext.SaveChangesAsync();
            return RedirectToAction("Index");
        } 
    } 
}
