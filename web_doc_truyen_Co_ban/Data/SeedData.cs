using Microsoft.EntityFrameworkCore;
using web_doc_truyen_Co_ban.Models;

namespace web_doc_truyen_Co_ban.Data
{
    public class SeedData
    {
        public static void SeedingData(ApplicationDbContext _context)
        {
            _context.Database.Migrate();

            // ============================
            // SEED ROLES
            // ============================
            if (!_context.Roles.Any())
            {
                var roles = new List<RolesModel>
                {
                    new RolesModel
                    {
                        Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                        Name = "Admin",
                        Permissions = "[\"manage_users\",\"manage_stories\",\"manage_comments\",\"manage_roles\"]",
                        CreatedAt = DateTime.UtcNow
                    },
                    new RolesModel
                    {
                        Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                        Name = "Moderator",
                        Permissions = "[\"manage_stories\",\"manage_comments\"]",
                        CreatedAt = DateTime.UtcNow
                    },
                    new RolesModel
                    {
                        Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                        Name = "Member",
                        Permissions = "[\"read\",\"comment\",\"follow\",\"bookmark\"]",
                        CreatedAt = DateTime.UtcNow
                    },
                };

                _context.Roles.AddRange(roles);
                _context.SaveChanges();
            }

            // ============================
            // SEED CATEGORIES
            // ============================
            if (!_context.Categories.Any())
            {
                var categories = new List<CategoryModel>
                {
                    new CategoryModel
                    {
                        Id = Guid.Parse("c0000000-0000-0000-0000-000000000001"),
                        Name = "Tiên Hiệp",
                        Slug = "tien-hiep",
                        Description = "Truyện về tu tiên, luyện đan, phi thăng thành tiên",
                        DisplayOrder = 1,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new CategoryModel
                    {
                        Id = Guid.Parse("c0000000-0000-0000-0000-000000000002"),
                        Name = "Huyền Huyễn",
                        Slug = "huyen-huyen",
                        Description = "Truyện thế giới huyền ảo, ma pháp, kiếm sĩ",
                        DisplayOrder = 2,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new CategoryModel
                    {
                        Id = Guid.Parse("c0000000-0000-0000-0000-000000000003"),
                        Name = "Ngôn Tình",
                        Slug = "ngon-tinh",
                        Description = "Truyện tình cảm lãng mạn nam nữ",
                        DisplayOrder = 3,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new CategoryModel
                    {
                        Id = Guid.Parse("c0000000-0000-0000-0000-000000000004"),
                        Name = "Đô Thị",
                        Slug = "do-thi",
                        Description = "Truyện đời thường, cuộc sống hiện đại",
                        DisplayOrder = 4,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new CategoryModel
                    {
                        Id = Guid.Parse("c0000000-0000-0000-0000-000000000005"),
                        Name = "Kiếm Hiệp",
                        Slug = "kiem-hiep",
                        Description = "Truyện võ hiệp, giang hồ, kiếm khách",
                        DisplayOrder = 5,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                };

                _context.Categories.AddRange(categories);
                _context.SaveChanges();
            }

            // ============================
            // SEED TAGS
            // ============================
            if (!_context.Tags.Any())
            {
                var tags = new List<TagModel>
                {
                    new TagModel { Id = Guid.Parse("a0000000-0000-0000-0000-000000000001"), Name = "Xuyên Không",        Slug = "xuyen-khong",        CreatedAt = DateTime.UtcNow },
                    new TagModel { Id = Guid.Parse("a0000000-0000-0000-0000-000000000002"), Name = "Trùng Sinh",          Slug = "trung-sinh",         CreatedAt = DateTime.UtcNow },
                    new TagModel { Id = Guid.Parse("a0000000-0000-0000-0000-000000000003"), Name = "Hệ Thống",            Slug = "he-thong",           CreatedAt = DateTime.UtcNow },
                    new TagModel { Id = Guid.Parse("a0000000-0000-0000-0000-000000000004"), Name = "Dị Giới",             Slug = "di-gioi",            CreatedAt = DateTime.UtcNow },
                    new TagModel { Id = Guid.Parse("a0000000-0000-0000-0000-000000000005"), Name = "Mạnh Mẽ Từ Đầu",     Slug = "manh-me-tu-dau",     CreatedAt = DateTime.UtcNow },
                    new TagModel { Id = Guid.Parse("a0000000-0000-0000-0000-000000000006"), Name = "Phế Vật Nghịch Tập",  Slug = "phe-vat-nghich-tap", CreatedAt = DateTime.UtcNow },
                };

                _context.Tags.AddRange(tags);
                _context.SaveChanges();
            }

            // ============================
            // SEED USERS
            // ============================
            if (!_context.Users.Any())
            {
                var users = new List<UsersModel>
                {
                    new UsersModel
                    {
                        Id = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                        Username = "admin",
                        Email = "admin@webdoctruyen.vn",
                        PasswordHash = "$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj6o8dX1ZBHG",
                        DisplayName = "Quản Trị Viên",
                        AvatarUrl = "/images/avatars/admin.png",
                        Bio = "Quản trị viên hệ thống WebDocTruyen",
                        RoleId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                        Status = 1,
                        EmailVerified = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new UsersModel
                    {
                        Id = Guid.Parse("20000000-0000-0000-0000-000000000002"),
                        Username = "mod_trang",
                        Email = "trang.mod@webdoctruyen.vn",
                        PasswordHash = "$2a$12$KIx8QbWVHxkd0LHAkCOYz6TtxMQJqhN8/abcdBPj6o8dX1ZBHAA",
                        DisplayName = "Trang Moderator",
                        RoleId = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                        Status = 1,
                        EmailVerified = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new UsersModel
                    {
                        Id = Guid.Parse("20000000-0000-0000-0000-000000000003"),
                        Username = "nguyen_van_a",
                        Email = "nguyenvana@gmail.com",
                        PasswordHash = "$2a$12$XYZabcWVHxkd0LHAkCOYz6TtxMQJqhN8/xyzBPj6o8dX1ZBHBB",
                        DisplayName = "Nguyễn Văn A",
                        Bio = "Yêu thích đọc truyện tiên hiệp và huyền huyễn",
                        RoleId = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                        Status = 1,
                        EmailVerified = true,
                        TotalFollows = 2,
                        CreatedAt = DateTime.UtcNow.AddDays(-30),
                        UpdatedAt = DateTime.UtcNow
                    },
                    new UsersModel
                    {
                        Id = Guid.Parse("20000000-0000-0000-0000-000000000004"),
                        Username = "le_thi_b",
                        Email = "lethib@gmail.com",
                        PasswordHash = "$2a$12$ABCdefWVHxkd0LHAkCOYz6TtxMQJqhN8/defBPj6o8dX1ZBHCC",
                        DisplayName = "Lê Thị B",
                        RoleId = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                        Status = 1,
                        EmailVerified = false,
                        VerifyToken = "verify-token-le-thi-b-abc123",
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        UpdatedAt = DateTime.UtcNow
                    },
                };

                _context.Users.AddRange(users);
                _context.SaveChanges();
            }

            // ============================
            // SEED USER SESSIONS
            // ============================
            if (!_context.UsersSessions.Any())
            {
                var sessions = new List<UsersSessionModel>
                {
                    new UsersSessionModel
                    {
                        Id = Guid.NewGuid(),
                        UserId = Guid.Parse("20000000-0000-0000-0000-000000000003"),
                        RefreshToken = "rt_nguyen_van_a_abc123def456",
                        IpAddress = "113.161.72.10",
                        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/124.0",
                        ExpiresAt = DateTime.UtcNow.AddDays(30),
                        CreatedAt = DateTime.UtcNow
                    },
                    new UsersSessionModel
                    {
                        Id = Guid.NewGuid(),
                        UserId = Guid.Parse("20000000-0000-0000-0000-000000000004"),
                        RefreshToken = "rt_le_thi_b_xyz789uvw012",
                        IpAddress = "27.72.100.50",
                        UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0) Safari/604.1",
                        ExpiresAt = DateTime.UtcNow.AddDays(30),
                        CreatedAt = DateTime.UtcNow
                    },
                };

                _context.UsersSessions.AddRange(sessions);
                _context.SaveChanges();
            }

            // ============================
            // SEED STORIES
            // ============================
            if (!_context.Stories.Any())
            {
                var story1Id = Guid.Parse("b0000000-0000-0000-0000-000000000001");
                var story2Id = Guid.Parse("b0000000-0000-0000-0000-000000000002");
                var story3Id = Guid.Parse("b0000000-0000-0000-0000-000000000003");

                var stories = new List<StoryModel>
                {
                    new StoryModel
                    {
                        Id = story1Id,
                        Title = "Đấu Phá Thương Khung",
                        Slug = "dau-pha-thuong-khung",
                        AuthorId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                        CategoryId = Guid.Parse("c0000000-0000-0000-0000-000000000001"),
                        Description = "Thiếu niên Tiêu Viêm từ thiên tài trở thành phế vật, quyết tâm tu luyện trở lại đỉnh cao.",
                        OriginalAuthor = "Thiên Tàm Thổ Đậu",
                        Source = "Dịch Việt",
                        Status = 2,
                        IsFeatured = true,
                        TotalChapters = 3,
                        LastChapterAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new StoryModel
                    {
                        Id = story2Id,
                        Title = "Toàn Chức Pháp Sư",
                        Slug = "toan-chuc-phap-su",
                        AuthorId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                        CategoryId = Guid.Parse("c0000000-0000-0000-0000-000000000002"),
                        Description = "Mặc Phàm xuyên qua thế giới ma pháp, sở hữu khả năng học toàn bộ hệ ma pháp.",
                        OriginalAuthor = "Loạn",
                        Source = "Dịch Việt",
                        Status = 1,
                        IsFeatured = true,
                        TotalChapters = 2,
                        LastChapterAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new StoryModel
                    {
                        Id = story3Id,
                        Title = "Thần Mộ",
                        Slug = "than-mo",
                        AuthorId = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                        CategoryId = Guid.Parse("c0000000-0000-0000-0000-000000000001"),
                        Description = "Diệp Thần bước vào con đường tu luyện từ một nghĩa địa thần bí, vươn lên đỉnh cao.",
                        OriginalAuthor = "Thần Đông",
                        Source = "Dịch Việt",
                        Status = 2,
                        IsFeatured = false,
                        TotalChapters = 2,
                        LastChapterAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                };

                _context.Stories.AddRange(stories);
                _context.SaveChanges();

                // Gắn Tags vào Stories (Many-to-Many)
                var tagPheVat = _context.Tags.Find(Guid.Parse("a0000000-0000-0000-0000-000000000006"));
                var tagHeThong = _context.Tags.Find(Guid.Parse("a0000000-0000-0000-0000-000000000003"));
                var tagDiGioi = _context.Tags.Find(Guid.Parse("a0000000-0000-0000-0000-000000000004"));
                var tagManhMe = _context.Tags.Find(Guid.Parse("a0000000-0000-0000-0000-000000000005"));

                var dbStory1 = _context.Stories.Include(s => s.Tags).First(s => s.Id == story1Id);
                dbStory1.Tags.Add(tagPheVat!);
                dbStory1.Tags.Add(tagHeThong!);

                var dbStory2 = _context.Stories.Include(s => s.Tags).First(s => s.Id == story2Id);
                dbStory2.Tags.Add(tagDiGioi!);
                dbStory2.Tags.Add(tagManhMe!);

                var dbStory3 = _context.Stories.Include(s => s.Tags).First(s => s.Id == story3Id);
                dbStory3.Tags.Add(tagManhMe!);

                _context.SaveChanges();
            }

            // ============================
            // SEED CHAPTERS
            // ============================
            if (!_context.Chapters.Any())
            {
                var chapters = new List<ChapterModel>
                {
                    // Đấu Phá Thương Khung
                    new ChapterModel
                    {
                        Id = Guid.Parse("e0000000-0000-0000-0000-000000000001"),
                        StoryId = Guid.Parse("b0000000-0000-0000-0000-000000000001"),
                        ChapterNumber = 1,
                        Title = "Phế Vật",
                        Content = "Tiêu Viêm ngồi một mình trong căn phòng tối tăm, nhìn chằm chằm vào bàn tay mình. Ba năm trước, hắn là thiên tài được cả tộc kỳ vọng...",
                        WordCount = 2800,
                        Status = 1,
                        PublishedAt = DateTime.UtcNow.AddDays(-10),
                        CreatedAt = DateTime.UtcNow.AddDays(-10),
                        UpdatedAt = DateTime.UtcNow.AddDays(-10)
                    },
                    new ChapterModel
                    {
                        Id = Guid.Parse("e0000000-0000-0000-0000-000000000002"),
                        StoryId = Guid.Parse("b0000000-0000-0000-0000-000000000001"),
                        ChapterNumber = 2,
                        Title = "Nữ Thần Trong Nhẫn",
                        Content = "Ánh sáng kỳ lạ tỏa ra từ chiếc nhẫn đen, một bóng hình phụ nữ dần xuất hiện trước mắt Tiêu Viêm...",
                        WordCount = 3100,
                        Status = 1,
                        PublishedAt = DateTime.UtcNow.AddDays(-8),
                        CreatedAt = DateTime.UtcNow.AddDays(-8),
                        UpdatedAt = DateTime.UtcNow.AddDays(-8)
                    },
                    new ChapterModel
                    {
                        Id = Guid.Parse("e0000000-0000-0000-0000-000000000003"),
                        StoryId = Guid.Parse("b0000000-0000-0000-0000-000000000001"),
                        ChapterNumber = 3,
                        Title = "Bắt Đầu Tu Luyện",
                        Content = "Dưới sự hướng dẫn của Dược Lão, Tiêu Viêm lần đầu tiên cảm nhận được dòng chảy của đấu khí trong cơ thể...",
                        WordCount = 2950,
                        Status = 1,
                        PublishedAt = DateTime.UtcNow.AddDays(-6),
                        CreatedAt = DateTime.UtcNow.AddDays(-6),
                        UpdatedAt = DateTime.UtcNow.AddDays(-6)
                    },
                    // Toàn Chức Pháp Sư
                    new ChapterModel
                    {
                        Id = Guid.Parse("e0000000-0000-0000-0000-000000000004"),
                        StoryId = Guid.Parse("b0000000-0000-0000-0000-000000000002"),
                        ChapterNumber = 1,
                        Title = "Thức Tỉnh Ma Pháp",
                        Content = "Mặc Phàm mở mắt ra, xung quanh là một thế giới hoàn toàn xa lạ. Hắn nhớ rõ mình vừa ngủ gật trong giờ học...",
                        WordCount = 3200,
                        Status = 1,
                        PublishedAt = DateTime.UtcNow.AddDays(-5),
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        UpdatedAt = DateTime.UtcNow.AddDays(-5)
                    },
                    new ChapterModel
                    {
                        Id = Guid.Parse("e0000000-0000-0000-0000-000000000005"),
                        StoryId = Guid.Parse("b0000000-0000-0000-0000-000000000002"),
                        ChapterNumber = 2,
                        Title = "Học Viện Ma Pháp",
                        Content = "Học viện Pháp Sư Đế Quốc hiện ra trước mắt, những tòa tháp cao vút tỏa ra hào quang ma pháp rực rỡ...",
                        WordCount = 2700,
                        Status = 1,
                        PublishedAt = DateTime.UtcNow.AddDays(-3),
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        UpdatedAt = DateTime.UtcNow.AddDays(-3)
                    },
                    // Thần Mộ
                    new ChapterModel
                    {
                        Id = Guid.Parse("e0000000-0000-0000-0000-000000000006"),
                        StoryId = Guid.Parse("b0000000-0000-0000-0000-000000000003"),
                        ChapterNumber = 1,
                        Title = "Nghĩa Địa Thần Bí",
                        Content = "Diệp Thần lạc vào một nghĩa địa cổ xưa không có trên bất kỳ bản đồ nào. Những tấm bia mộ khắc tên những vị thần đã từng tồn tại...",
                        WordCount = 3050,
                        Status = 1,
                        PublishedAt = DateTime.UtcNow.AddDays(-7),
                        CreatedAt = DateTime.UtcNow.AddDays(-7),
                        UpdatedAt = DateTime.UtcNow.AddDays(-7)
                    },
                    new ChapterModel
                    {
                        Id = Guid.Parse("e0000000-0000-0000-0000-000000000007"),
                        StoryId = Guid.Parse("b0000000-0000-0000-0000-000000000003"),
                        ChapterNumber = 2,
                        Title = "Kế Thừa Di Sản",
                        Content = "Từ trong tấm bia mộ đầu tiên, một luồng ý chí hùng mạnh ập vào tâm trí Diệp Thần, mang theo ký ức của một vị thần cổ đại...",
                        WordCount = 3300,
                        Status = 1,
                        PublishedAt = DateTime.UtcNow.AddDays(-4),
                        CreatedAt = DateTime.UtcNow.AddDays(-4),
                        UpdatedAt = DateTime.UtcNow.AddDays(-4)
                    },
                };

                _context.Chapters.AddRange(chapters);
                _context.SaveChanges();
            }

            // ============================
            // SEED FOLLOWS
            // ============================
            if (!_context.Follows.Any())
            {
                // Kiểm tra Stories và Users tồn tại trước khi seed
                var story1 = _context.Stories.Find(Guid.Parse("b0000000-0000-0000-0000-000000000001"));
                var story2 = _context.Stories.Find(Guid.Parse("b0000000-0000-0000-0000-000000000002"));
                var story3 = _context.Stories.Find(Guid.Parse("b0000000-0000-0000-0000-000000000003"));
                var user3 = _context.Users.Find(Guid.Parse("20000000-0000-0000-0000-000000000003"));
                var user4 = _context.Users.Find(Guid.Parse("20000000-0000-0000-0000-000000000004"));

                if (story1 != null && story2 != null && story3 != null && user3 != null && user4 != null)
                {
                    var follows = new List<FollowModel>
                    {
                        new FollowModel
                        {
                            Id = Guid.NewGuid(),
                            UserId  = user3.Id,
                            StoryId = story1.Id,
                            CreatedAt = DateTime.UtcNow.AddDays(-20)
                        },
                        new FollowModel
                        {
                            Id = Guid.NewGuid(),
                            UserId  = user3.Id,
                            StoryId = story2.Id,
                            CreatedAt = DateTime.UtcNow.AddDays(-10)
                        },
                        new FollowModel
                        {
                            Id = Guid.NewGuid(),
                            UserId  = user4.Id,
                            StoryId = story1.Id,
                            CreatedAt = DateTime.UtcNow.AddDays(-3)
                        },
                    };

                    _context.Follows.AddRange(follows);
                    _context.SaveChanges();
                }
            }

            // ============================
            // SEED BOOKMARKS
            // ============================
            if (!_context.Bookmarks.Any())
            {                
                var story1 = _context.Stories.Find(Guid.Parse("b0000000-0000-0000-0000-000000000001"));
                var story2 = _context.Stories.Find(Guid.Parse("b0000000-0000-0000-0000-000000000002"));
                var story3 = _context.Stories.Find(Guid.Parse("b0000000-0000-0000-0000-000000000003"));
                var chapter2 = _context.Chapters.Find(Guid.Parse("e0000000-0000-0000-0000-000000000002"));
                var chapter5 = _context.Chapters.Find(Guid.Parse("e0000000-0000-0000-0000-000000000005"));
                var chapter6 = _context.Chapters.Find(Guid.Parse("e0000000-0000-0000-0000-000000000006"));
                var user3 = _context.Users.Find(Guid.Parse("20000000-0000-0000-0000-000000000003"));
                var user4 = _context.Users.Find(Guid.Parse("20000000-0000-0000-0000-000000000004"));

                if (story1 != null && story2 != null && story3 != null
                    && chapter2 != null && chapter5 != null && chapter6 != null
                    && user3 != null && user4 != null)
                {
                    var bookmarks = new List<BookmarkModel>
                    {
                        new BookmarkModel
                        {
                            Id = Guid.NewGuid(),
                            UserId         = user3.Id,
                            StoryId        = story1.Id,
                            ChapterId      = chapter2.Id,
                            ChapterNumber  = 2,
                            ScrollPosition = 45,
                            UpdatedAt      = DateTime.UtcNow.AddDays(-1)
                        },
                        new BookmarkModel
                        {
                            Id = Guid.NewGuid(),
                            UserId         = user4.Id,
                            StoryId        = story3.Id,
                            ChapterId      = chapter6.Id,
                            ChapterNumber  = 1,
                            ScrollPosition = 0,
                            UpdatedAt      = DateTime.UtcNow
                        },
                        new BookmarkModel
                        {
                            Id = Guid.NewGuid(),
                            UserId         = user3.Id,
                            StoryId        = story2.Id,
                            ChapterId      = chapter5.Id,
                            ChapterNumber  = 2,
                            ScrollPosition = 80,
                            UpdatedAt      = DateTime.UtcNow
                        },
                    };

                    _context.Bookmarks.AddRange(bookmarks);
                    _context.SaveChanges();
                }
            }

            // ============================
            // SEED RATINGS
            // ============================
            if (!_context.Ratings.Any())
            {
                var story1 = _context.Stories.Find(Guid.Parse("b0000000-0000-0000-0000-000000000001"));
                var story2 = _context.Stories.Find(Guid.Parse("b0000000-0000-0000-0000-000000000002"));
                var story3 = _context.Stories.Find(Guid.Parse("b0000000-0000-0000-0000-000000000003"));
                var user3 = _context.Users.Find(Guid.Parse("20000000-0000-0000-0000-000000000003"));
                var user4 = _context.Users.Find(Guid.Parse("20000000-0000-0000-0000-000000000004"));

                if (story1 != null && story2 != null && story3 != null && user3 != null && user4 != null)
                {
                    var ratings = new List<RatingModel>
                    {
                        new RatingModel
                        {
                            Id = Guid.NewGuid(),
                            UserId  = user3.Id,
                            StoryId = story1.Id,
                            Score = 5,
                            CreatedAt = DateTime.UtcNow.AddDays(-5),
                            UpdatedAt = DateTime.UtcNow.AddDays(-5)
                        },
                        new RatingModel
                        {
                            Id = Guid.NewGuid(),
                            UserId  = user4.Id,
                            StoryId = story1.Id,
                            Score = 4,
                            CreatedAt = DateTime.UtcNow.AddDays(-2),
                            UpdatedAt = DateTime.UtcNow.AddDays(-2)
                        },
                        new RatingModel
                        {
                            Id = Guid.NewGuid(),
                            UserId  = user3.Id,
                            StoryId = story2.Id,
                            Score = 5,
                            CreatedAt = DateTime.UtcNow.AddDays(-1),
                            UpdatedAt = DateTime.UtcNow.AddDays(-1)
                        },
                        new RatingModel
                        {
                            Id = Guid.NewGuid(),
                            UserId  = user4.Id,
                            StoryId = story3.Id,
                            Score = 4,
                            CreatedAt = DateTime.UtcNow.AddDays(-3),
                            UpdatedAt = DateTime.UtcNow.AddDays(-3)
                        },
                    };

                    _context.Ratings.AddRange(ratings);
                    _context.SaveChanges();

                    // Cập nhật RatingSum / RatingCount / RatingAvg cho Stories
                    var storyIds = ratings.Select(r => r.StoryId).Distinct();
                    foreach (var storyId in storyIds)
                    {
                        var story = _context.Stories.Find(storyId);
                        if (story == null) continue;
                        var storyRatings = _context.Ratings.Where(r => r.StoryId == storyId).ToList();
                        story.RatingCount = storyRatings.Count;
                        story.RatingSum = storyRatings.Sum(r => r.Score);
                        story.RatingAvg = story.RatingCount > 0
                            ? Math.Round((decimal)story.RatingSum / story.RatingCount, 2)
                            : 0;
                    }
                    _context.SaveChanges();
                }
            }

            // ============================
            // SEED CHAPTER VIEWS
            // ============================
            if (!_context.ChapterViews.Any())
            {
                var allChapters = _context.Chapters.ToList();
                if (allChapters.Any())
                {
                    var chapterViews = new List<ChapterViewModel>();

                    foreach (var chapter in allChapters)
                    {
                        // User đã đăng nhập
                        chapterViews.Add(new ChapterViewModel
                        {
                            Id = Guid.NewGuid(),
                            ChapterId = chapter.Id,
                            StoryId = chapter.StoryId,
                            UserId = Guid.Parse("20000000-0000-0000-0000-000000000003"),
                            IpAddress = "113.161.72.10",
                            ViewedAt = DateTime.UtcNow.AddDays(-7)
                        });
                        chapterViews.Add(new ChapterViewModel
                        {
                            Id = Guid.NewGuid(),
                            ChapterId = chapter.Id,
                            StoryId = chapter.StoryId,
                            UserId = Guid.Parse("20000000-0000-0000-0000-000000000004"),
                            IpAddress = "27.72.100.50",
                            ViewedAt = DateTime.UtcNow.AddDays(-3)
                        });
                        // Khách vãng lai
                        chapterViews.Add(new ChapterViewModel
                        {
                            Id = Guid.NewGuid(),
                            ChapterId = chapter.Id,
                            StoryId = chapter.StoryId,
                            UserId = null,
                            IpAddress = "14.161.30.99",
                            ViewedAt = DateTime.UtcNow.AddDays(-1)
                        });
                    }

                    _context.ChapterViews.AddRange(chapterViews);
                    _context.SaveChanges();

                    // Cập nhật ViewCount cho Chapters và TotalViews cho Stories
                    foreach (var chapter in allChapters)
                        chapter.ViewCount = chapterViews.Count(cv => cv.ChapterId == chapter.Id);

                    var allStories = _context.Stories.ToList();
                    foreach (var story in allStories)
                        story.TotalViews = chapterViews.Count(cv => cv.StoryId == story.Id);

                    _context.SaveChanges();
                }
            }

            // ============================
            // SEED READING HISTORY
            // ============================
            if (!_context.ReadingHistories.Any())
            {
                var story1 = _context.Stories.Find(Guid.Parse("b0000000-0000-0000-0000-000000000001"));
                var story2 = _context.Stories.Find(Guid.Parse("b0000000-0000-0000-0000-000000000002"));
                var story3 = _context.Stories.Find(Guid.Parse("b0000000-0000-0000-0000-000000000003"));
                var chapter3 = _context.Chapters.Find(Guid.Parse("e0000000-0000-0000-0000-000000000003"));
                var chapter5 = _context.Chapters.Find(Guid.Parse("e0000000-0000-0000-0000-000000000005"));
                var chapter6 = _context.Chapters.Find(Guid.Parse("e0000000-0000-0000-0000-000000000006"));
                var user3 = _context.Users.Find(Guid.Parse("20000000-0000-0000-0000-000000000003"));
                var user4 = _context.Users.Find(Guid.Parse("20000000-0000-0000-0000-000000000004"));

                if (story1 != null && story2 != null && story3 != null
                    && chapter3 != null && chapter5 != null && chapter6 != null
                    && user3 != null && user4 != null)
                {
                    var histories = new List<ReadingHistoryModel>
                    {
                        new ReadingHistoryModel
                        {
                            Id = Guid.NewGuid(),
                            UserId        = user3.Id,
                            StoryId       = story1.Id,
                            ChapterId     = chapter3.Id,
                            ChapterNumber = 3,
                            LastReadAt    = DateTime.UtcNow.AddHours(-2)
                        },
                        new ReadingHistoryModel
                        {
                            Id = Guid.NewGuid(),
                            UserId        = user3.Id,
                            StoryId       = story2.Id,
                            ChapterId     = chapter5.Id,
                            ChapterNumber = 2,
                            LastReadAt    = DateTime.UtcNow.AddDays(-1)
                        },
                        new ReadingHistoryModel
                        {
                            Id = Guid.NewGuid(),
                            UserId        = user4.Id,
                            StoryId       = story3.Id,
                            ChapterId     = chapter6.Id,
                            ChapterNumber = 1,
                            LastReadAt    = DateTime.UtcNow.AddDays(-2)
                        }
                    };

                    _context.ReadingHistories.AddRange(histories);
                    _context.SaveChanges();
                }
            }
            if (!_context.MembershipPlans.Any())
            {
                var plans = new List<MembershipPlan>
                {
                    new MembershipPlan
                    {
                       
                        Name = "Hội viên Bạc",
                        Description = "Đọc truyện không giới hạn, không quảng cáo",
                        Price = 29000,
                        DurationDays = 30,
                        IsActive = true,
                        ChapterUnlockPerDay = 0,
                        NoAds = true,
                        EarlyAccess = false,
                        CreatedAt = DateTime.UtcNow
                    },

                    new MembershipPlan
                    {
                        
                        Name = "Hội viên Vàng",
                        Description = "Không quảng cáo + ưu tiên hỗ trợ",
                        Price = 59000,
                        DurationDays = 30,
                        IsActive = true,
                        ChapterUnlockPerDay = 0,
                        NoAds = true,
                        EarlyAccess = false,
                        CreatedAt = DateTime.UtcNow
                    },

                    new MembershipPlan
                    {
                       
                        Name = "Hội viên Kim Cương",
                        Description = "Đọc sớm chương VIP + toàn bộ đặc quyền",
                        Price = 99000,
                        DurationDays = 30,
                        IsActive = true,
                        ChapterUnlockPerDay = 0,
                        NoAds = true,
                        EarlyAccess = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                _context.MembershipPlans.AddRange(plans);
                _context.SaveChanges();
            }
        }
    }
    
}