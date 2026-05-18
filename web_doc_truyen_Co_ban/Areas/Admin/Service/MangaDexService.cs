using System.Net.Http;
using System.Text.Json;
using web_doc_truyen_Co_ban.Models;

namespace web_doc_truyen_Co_ban.Areas.Admin.Service
{
    public class MangaDexService
    {
        private readonly HttpClient _http;
        private const string BaseUrl = "https://api.mangadex.org";

        public MangaDexService(HttpClient http)
        {
            _http = http;
            _http.DefaultRequestHeaders.Add("User-Agent", "WebDocTruyen/1.0");
        }

        // ─── Search: tìm theo tên ───────────────────────────────────────────────
        public async Task<List<MangaDexResult>> SearchAsync(string title, int limit = 10)
        {
            var url = $"{BaseUrl}/manga?title={Uri.EscapeDataString(title)}&limit={limit}" +
                      "&includes[]=author&includes[]=cover_art" +
                      "&contentRating[]=safe&contentRating[]=suggestive&contentRating[]=erotica&contentRating[]=pornographic";

            var json = await _http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);

            var results = new List<MangaDexResult>();
            if (!doc.RootElement.TryGetProperty("data", out var data)) return results;

            foreach (var item in data.EnumerateArray())
            {
                var r = ParseMangaItem(item);
                if (r != null) results.Add(r);
            }
            return results;
        }

        // ─── Get by MangaDex ID ─────────────────────────────────────────────────
        //public async Task<MangaDexResult?> GetByIdAsync(string mangaDexId)
        //{
        //    var url = $"{BaseUrl}/manga/{mangaDexId}?includes[]=author&includes[]=cover_art";
        //    var json = await _http.GetStringAsync(url);
        //    using var doc = JsonDocument.Parse(json);

        //    if (!doc.RootElement.TryGetProperty("data", out var data)) return null;
        //    return ParseMangaItem(data);
        //}
        public async Task<MangaDexResult?> GetByIdAsync(string mangaDexId)
        {
            try
            {
                var url = $"https://api.mangadex.org/manga/{mangaDexId}?includes[]=author&includes[]=cover_art";

                var response = await _http.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                Console.WriteLine("URL: " + url);
                Console.WriteLine("STATUS: " + response.StatusCode);
                Console.WriteLine("BODY: " + json);

                if (!response.IsSuccessStatusCode)
                    return null;

                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("data", out var data))
                    return null;

                return ParseMangaItem(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
                return null;
            }
        }

        // ─── Parse 1 manga item → MangaDexResult ───────────────────────────────
        private MangaDexResult? ParseMangaItem(JsonElement item)
        {
            try
            {
                var mangaDexId = item.GetProperty("id").GetString() ?? "";
                var attrs = item.GetProperty("attributes");

                // Title: ưu tiên vi > en > key đầu tiên
                var titleObj = attrs.GetProperty("title");
                var title = GetLocalizedText(titleObj, "vi")
                         ?? GetLocalizedText(titleObj, "en")
                         ?? GetFirstString(titleObj)
                         ?? "(Không có tên)";

                // Description
                string? description = null;
                if (attrs.TryGetProperty("description", out var descObj))
                {
                    description = GetLocalizedText(descObj, "vi")
                               ?? GetLocalizedText(descObj, "en")
                               ?? GetFirstString(descObj);
                }

                // Status: ongoing / completed / hiatus / cancelled
                string? status = null;
                if (attrs.TryGetProperty("status", out var statusProp))
                    status = statusProp.GetString();

                // Year
                int? year = null;
                if (attrs.TryGetProperty("year", out var yearProp) && yearProp.ValueKind == JsonValueKind.Number)
                    year = yearProp.GetInt32();

                // Content Rating
                string? contentRating = null;
                if (attrs.TryGetProperty("contentRating", out var crProp))
                    contentRating = crProp.GetString();

                // Tags → tên tag tiếng Anh
                var tagNames = new List<string>();
                if (attrs.TryGetProperty("tags", out var tagsArr))
                {
                    foreach (var tag in tagsArr.EnumerateArray())
                    {
                        if (tag.TryGetProperty("attributes", out var tagAttrs)
                            && tagAttrs.TryGetProperty("name", out var tagName))
                        {
                            var name = GetLocalizedText(tagName, "en") ?? GetFirstString(tagName);
                            if (!string.IsNullOrEmpty(name)) tagNames.Add(name);
                        }
                    }
                }

                // Relationships: author + cover_art
                string? authorName = null;
                string? coverFileName = null;

                if (item.TryGetProperty("relationships", out var rels))
                {
                    foreach (var rel in rels.EnumerateArray())
                    {
                        var type = rel.TryGetProperty("type", out var t) ? t.GetString() : null;

                        if (type == "author" && rel.TryGetProperty("attributes", out var authAttrs))
                        {
                            if (authAttrs.TryGetProperty("name", out var nameProp))
                                authorName = nameProp.GetString();
                        }

                        if (type == "cover_art" && rel.TryGetProperty("attributes", out var coverAttrs))
                        {
                            if (coverAttrs.TryGetProperty("fileName", out var fn))
                                coverFileName = fn.GetString();
                        }
                    }
                }

                // Cover URL: https://uploads.mangadex.org/covers/{mangaId}/{fileName}
                string? coverUrl = null;
                if (!string.IsNullOrEmpty(coverFileName))
                    coverUrl = $"https://uploads.mangadex.org/covers/{mangaDexId}/{coverFileName}";

                return new MangaDexResult
                {
                    MangaDexId = mangaDexId,
                    Title = title,
                    Description = description,
                    AuthorName = authorName,
                    CoverUrl = coverUrl,
                    Status = status,
                    Year = year,
                    ContentRating = contentRating,
                    Tags = tagNames
                };
            }
            catch
            {
                return null;
            }
        }

        // ─── Map MangaDexResult → StoryModel ───────────────────────────────────
        public StoryModel MapToStoryModel(MangaDexResult r)
        {
            // Tạo slug từ title
            var slug = r.Title
                .ToLower()
                .Normalize(System.Text.NormalizationForm.FormD)
                .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                            != System.Globalization.UnicodeCategory.NonSpacingMark)
                .Aggregate("", (s, c) => s + c)
                .Replace(" ", "-")
                .Trim('-');

            return new StoryModel
            {
                // ── Có data từ API ──────────────────────────────
                Title = r.Title,
                Slug = slug,
                Description = r.Description ?? "",
                OriginalAuthor = r.AuthorName ?? "",
                CoverImageUrl = r.CoverUrl ?? "",
                Source = $"https://mangadex.org/title/{r.MangaDexId}",
                //Status = MapStatus(r.Status),
                Status = r.Status switch
                {
                    "ongoing" => (byte)1,
                    "completed" => (byte)2,
                    "hiatus" => (byte)3,
                    "cancelled" => (byte)4,
                    _ => (byte)1
                },

                // ── Để trống, admin tự điền ─────────────────────
                CategoryId = Guid.Empty,    // admin chọn category
                // Tags sẽ được map riêng ở controller

                // ── Mặc định ────────────────────────────────────      
                IsFeatured = false,
                IsVip = false,
                Is18Plus = r.ContentRating is "erotica" or "pornographic",

                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
        }

        // ─── Helper ────────────────────────────────────────────────────────────
        private static string? GetLocalizedText(JsonElement obj, string lang)
        {
            if (obj.TryGetProperty(lang, out var val) && val.ValueKind == JsonValueKind.String)
                return val.GetString();
            return null;
        }

        private static string? GetFirstString(JsonElement obj)
        {
            foreach (var prop in obj.EnumerateObject())
                if (prop.Value.ValueKind == JsonValueKind.String)
                    return prop.Value.GetString();
            return null;
        }

        private static string MapStatus(string? s) => s switch
        {
            "ongoing" => "Đang tiến hành",
            "completed" => "Hoàn thành",
            "hiatus" => "Tạm dừng",
            "cancelled" => "Đã hủy",
            _ => "Đang tiến hành"
        };

        public async Task<List<ChapterModel>> GetChaptersAsync(string mangaDexId, Guid storyId)
        {
            var rawList = new List<MangaDexChapterResult>();
            int offset = 0;
            const int limit = 100;

            while (true)
            {
                var url = $"{BaseUrl}/chapter" +
                          $"?manga={mangaDexId}" +
                          $"&limit={limit}&offset={offset}" +
                          $"&order[chapter]=asc" +
                          $"&contentRating[]=safe&contentRating[]=suggestive" +
                          $"&contentRating[]=erotica&contentRating[]=pornographic";

                var response = await _http.GetAsync(url);
                if (!response.IsSuccessStatusCode) break;

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("data", out var data)) break;

                var items = data.EnumerateArray().ToList();
                if (items.Count == 0) break;

                foreach (var item in items)
                {
                    var r = ParseChapterItem(item); // ← bỏ storyId
                    if (r != null) rawList.Add(r);
                }

                int total = doc.RootElement.TryGetProperty("total", out var t) ? t.GetInt32() : 0;
                offset += limit;
                if (offset >= total) break;
            }

            // Dedup: ưu tiên vi → en → bất kỳ
            return rawList
                .GroupBy(c => c.ChapterNumber)
                .Select(g =>
                    g.FirstOrDefault(c => c.Language == "vi") ??
                    g.FirstOrDefault(c => c.Language == "en") ??
                    g.First())
                .OrderBy(c => c.ChapterNumber)
                .Select(r => MapToChapterModel(r, storyId)) // ← map sang ChapterModel
                .ToList();
        }

        private MangaDexChapterResult? ParseChapterItem(JsonElement item)
        {
            try
            {
                var attrs = item.GetProperty("attributes");

                string? chapterStr = null;
                if (attrs.TryGetProperty("chapter", out var chProp) && chProp.ValueKind == JsonValueKind.String)
                    chapterStr = chProp.GetString();

                if (!decimal.TryParse(chapterStr,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var chapterNumber))
                    return null;

                string? title = null;
                if (attrs.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
                    title = titleProp.GetString();

                DateTime? publishedAt = null;
                if (attrs.TryGetProperty("publishAt", out var pubProp) && pubProp.ValueKind == JsonValueKind.String)
                    if (DateTime.TryParse(pubProp.GetString(), out var dt))
                        publishedAt = dt.ToUniversalTime();

                int pages = 0;
                if (attrs.TryGetProperty("pages", out var pagesProp) && pagesProp.ValueKind == JsonValueKind.Number)
                    pages = pagesProp.GetInt32();

                string? language = null;
                if (attrs.TryGetProperty("translatedLanguage", out var langProp))
                    language = langProp.GetString();

                return new MangaDexChapterResult
                {
                    ChapterNumber = chapterNumber,
                    Title = string.IsNullOrWhiteSpace(title) ? $"Chapter {chapterNumber}" : title,
                    Language = language,
                    Pages = pages,
                    PublishedAt = publishedAt
                };
            }
            catch
            {
                return null;
            }
        }

        public ChapterModel MapToChapterModel(MangaDexChapterResult r, Guid storyId)
        {
            return new ChapterModel
            {
                Id = Guid.NewGuid(),
                StoryId = storyId,
                ChapterNumber = r.ChapterNumber,
                Title = r.Title,
                Content = null,
                WordCount = r.Pages,
                Status = 1,
                IsLocked = false,
                UnlockPrice = 0,
                ViewCount = 0,
                PublishedAt = r.PublishedAt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
        }
        //public async Task<string> DownloadCoverAsync(string? coverUrl, IWebHostEnvironment env)
        //{
        //    if (string.IsNullOrEmpty(coverUrl) || !coverUrl.StartsWith("http"))
        //        return coverUrl ?? "";

        //    try
        //    {
        //        using var http = new HttpClient();
        //        http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        //        http.DefaultRequestHeaders.Add("Referer", "https://mangadex.org/");

        //        var bytes = await http.GetByteArrayAsync(coverUrl);
        //        var ext = Path.GetExtension(coverUrl.Split('?')[0]);
        //        if (string.IsNullOrEmpty(ext)) ext = ".jpg";

        //        var fileName = Guid.NewGuid() + ext;
        //        var folder = Path.Combine(env.WebRootPath, "assets", "img");
        //        Directory.CreateDirectory(folder);
        //        await File.WriteAllBytesAsync(Path.Combine(folder, fileName), bytes);

        //        return "/assets/img/" + fileName;
        //    }
        //    catch
        //    {
        //        return coverUrl; // fallback giữ URL gốc
        //    }
        //}
        public async Task<string> DownloadCoverAsync(string imageUrl, IWebHostEnvironment env)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imageUrl))
                    return "/assets/img/default-cover.jpg";

                using var http = new HttpClient();

                http.DefaultRequestHeaders.Add(
                    "User-Agent",
                    "Mozilla/5.0"
                );

                var bytes = await http.GetByteArrayAsync(imageUrl);

                // lấy extension thật
                var ext = Path.GetExtension(
                    imageUrl.Split('?')[0]
                );

                if (string.IsNullOrWhiteSpace(ext))
                    ext = ".jpg";

                var fileName = Guid.NewGuid() + ext;

                var folder = Path.Combine(
                    env.WebRootPath,
                    "img"
                );

                Directory.CreateDirectory(folder);

                var filePath = Path.Combine(folder, fileName);

                await File.WriteAllBytesAsync(filePath, bytes);

                return "/img/" + fileName;
            }
            catch (Exception ex)
            {
                Console.WriteLine("DOWNLOAD COVER ERROR:");
                Console.WriteLine(ex.Message);

                return "/img/default-cover.jpg";
            }
        }

    }





    // DTO trung gian: dữ liệu thô từ MangaDex
    public class MangaDexResult
    {
        public string MangaDexId { get; set; } = "";
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public string? AuthorName { get; set; }
        public string? CoverUrl { get; set; }
        public string? Status { get; set; }
        public int? Year { get; set; }
        public string? ContentRating { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public class MangaDexChapterResult
    {
        public decimal ChapterNumber { get; set; }
        public string? Title { get; set; }
        public string? Language { get; set; }
        public int Pages { get; set; }
        public DateTime? PublishedAt { get; set; }
    }
}