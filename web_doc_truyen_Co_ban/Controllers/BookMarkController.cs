using AspNetCoreGeneratedDocument;
using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml.Spreadsheet;
using LinqToDB.Async;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.DTO;
using web_doc_truyen_Co_ban.Models;

namespace web_doc_truyen_Co_ban.Controllers
{
    public class BookMarkController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;
        public BookMarkController(ApplicationDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }
        [HttpGet]
        public async Task<IActionResult> GetByStory(Guid storyId)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var bookmarks = await _dbcontext.Bookmarks
                .Where(b => b.UserId == userId && b.StoryId == storyId)
                .OrderByDescending(b => b.UpdatedAt)
                .Select(b => new {
                    b.Id,
                    b.ChapterId,
                    b.ChapterNumber,
                    UpdatedAt = b.UpdatedAt.ToString("dd/MM/yy")
                })
                .ToListAsync();
            return Json(bookmarks);
        }
        public async Task< IActionResult> Index(Guid storyid)
        {
            var userid = Guid.Parse( User.FindFirstValue(ClaimTypes.NameIdentifier));
            var bookmark=await _dbcontext.Bookmarks
                .Where(c => c.UserId == userid && c.StoryId == storyid)
                .OrderByDescending(c=>c.UpdatedAt)
                .Select(c=>new BookMarkDto {
                    Id = c.Id,
                    ChapterId = c.ChapterId,
                    ChapterNumber = c.ChapterNumber,
                    UpdatedAt = c.UpdatedAt,
                    StoryName = c.Story != null ? c.Story.Title : null,
                    ChapterTitle = c.Chapter != null ? c.Chapter.Title : null

                }).FirstOrDefaultAsync();   
            return Json(bookmark);
        }
        [HttpPost]
        public async Task<IActionResult>CreateBookMark(Guid storieid,Guid chapterid)
        {
            var userid=Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var existed = await _dbcontext.Bookmarks
                .AnyAsync(c => c.UserId == userid
                && c.ChapterId == chapterid
                && c.StoryId == storieid);
            if (existed)
            {
                return Json(new { success = false, message = "Chương này đã được lưu rồi" });
            }
            var Chapterss=await _dbcontext.Chapters
                .Where(c=>c.Id==chapterid)
                .Select(c => new
                {
                   c.ChapterNumber,
                }).FirstOrDefaultAsync();
            if (Chapterss == null)
            {
                return Json(new { success = false, message = "Chuogn khong tồn tại" });
            }
            var bookmark = new BookmarkModel
            {
                Id = Guid.NewGuid(),
                UserId = userid,
                StoryId = storieid,
                ChapterId = chapterid,
                ChapterNumber = Chapterss.ChapterNumber,
                UpdatedAt = DateTime.UtcNow
            };
            _dbcontext.Bookmarks.Add(bookmark);
            await _dbcontext.SaveChangesAsync();

            return Json(new { success = true, message = $"Đã BookMark Chương{Chapterss.ChapterNumber}" });
        }
        [HttpPost]
        public async Task<IActionResult> Remove(Guid bookmarkid)
        {
            var userid=Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var bookmark = await _dbcontext.Bookmarks
                .FirstOrDefaultAsync(c=>c.Id==bookmarkid);
            if (bookmark == null)
            {
                return Json(new {success=false,message="Không Tìm thấy dòng muốn xóa"});
            }
            _dbcontext.Bookmarks.Remove(bookmark);
            await _dbcontext.SaveChangesAsync();
            return Json(new { success=true,Message="Xóa Thành Cong"});
        }
        [HttpPost]
        public async Task<IActionResult> RemoveALl(Guid bookmarkid)
        {
            var userid = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var bookmark = await _dbcontext.Bookmarks
                .Where(c => c.Id == bookmarkid).ToListAsync();
            if (bookmark == null)
            {
                return Json(new { success = false, message = "Không tìm thấy dòng muốn xóa" });
            }
            _dbcontext.Bookmarks.RemoveRange(bookmark);//range xóa hết lười quá

            await _dbcontext.SaveChangesAsync();
            return Json(new { success = true, Message = "Xóa  tất cả thành cong" });
        }

    }
}
