using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using web_doc_truyen_Co_ban.Models;
using web_doc_truyen_Co_ban.Models;

namespace web_doc_truyen_Co_ban.Data
{
    //public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
    //{
    //    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    //    {

    //    }

    //}
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options): base(options)
        {
        }

        // ---Tìa khoản---
        public DbSet<UsersModel> Users { get; set; }
        public DbSet<UsersSessionModel> UsersSessions { get; set; }
        //public DbSet<UserSessionViewModel> UsersSessionViewModel {  get; set; }
        //public DbSet<DeviceInfoViewModel> DeviceInfo { get; set; }


        // --- Nội dung truyện ---
        public DbSet<CategoryModel> Categories { get; set; }
        public DbSet<StoryModel> Stories { get; set; }
        public DbSet<ChapterModel> Chapters { get; set; }


        // --- Tags ) ---
        public DbSet<TagModel> Tags { get; set; }

        // --- Tương tác người dùng ---
        public DbSet<FollowModel> Follows { get; set; }
        public DbSet<BookmarkModel> Bookmarks { get; set; }
        public DbSet<RatingModel> Ratings { get; set; }
        public DbSet<ChapterViewModel> ChapterViews { get; set; }
        public DbSet<ReadingHistoryModel> ReadingHistories { get; set; }

        // --- Bình luận ---
        public DbSet<StoryCommentModel> StoryComments { get; set; }
        public DbSet<ChapterCommentModel> ChapterComments { get; set; }
        // --- Hệ thống ---
        public DbSet<RolesModel> Roles { get; set; }

        //thảo luân
        public DbSet<ThaoLuanDanhMucModel> ForumCategory { get; set; }
        public DbSet<BaiVietModel> Threads { get; set; }
        public DbSet<TraLoiBaiVietModel> Replies { get; set; }
        public DbSet<ThongBaoModel> ThongBaos { get; set; }
        //like
        public DbSet<LikeModel> Likes { get; set; }

        //hội viên
        public DbSet<MembershipPlan> MembershipPlans { get; set; }
        public DbSet<UserMembership> UserMemberships { get; set; }
        public DbSet<MembershipOrder> MembershipOrders { get; set; }

        public DbSet<CoinTransaction> CoinTransactions { get; set; }
        public DbSet<UnlockedChapter> UnlockedChapters   { get; set; }
        //báocao
        public DbSet<ReportModel> Reports { get; set; }

       // bắt log toàn model action
       public DbSet<ErrorLogAdminViewModel> ErrorsAdmin { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // -------------------------------------------------------
            // USERS SESSION: xóa User → xóa luôn Session (OK, 1 đường)
            // -------------------------------------------------------
            modelBuilder.Entity<UsersSessionModel>()
                .HasOne(s => s.User)
                .WithMany(u => u.Sessions)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // -------------------------------------------------------
            // STORY: AuthorId → Users
            // Dùng NoAction để tránh cycle (User → Story → Chapter → Comment...)
            // -------------------------------------------------------
            modelBuilder.Entity<StoryModel>()
                .HasOne(s => s.Author)
                .WithMany()
                .HasForeignKey(s => s.AuthorId)
                .OnDelete(DeleteBehavior.NoAction); // ← FIX: không cascade

            // -------------------------------------------------------
            // FOLLOW: UserId → Users
            // -------------------------------------------------------
            modelBuilder.Entity<FollowModel>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.NoAction); // ← FIX

            modelBuilder.Entity<FollowModel>()
                .HasOne(f => f.Story)
                .WithMany()
                .HasForeignKey(f => f.StoryId)
                .OnDelete(DeleteBehavior.NoAction); // ← FIX

            // -------------------------------------------------------
            // BOOKMARK
            // -------------------------------------------------------
            modelBuilder.Entity<BookmarkModel>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.NoAction); // ← FIX

            modelBuilder.Entity<BookmarkModel>()
                .HasOne(b => b.Story)
                .WithMany()
                .HasForeignKey(b => b.StoryId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<BookmarkModel>()
                .HasOne(b => b.Chapter)
                .WithMany()
                .HasForeignKey(b => b.ChapterId)
                .OnDelete(DeleteBehavior.SetNull); // Xóa chapter → ChapterId = NULL

            // -------------------------------------------------------
            // RATING
            // -------------------------------------------------------
            modelBuilder.Entity<RatingModel>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.NoAction); // ← FIX

            modelBuilder.Entity<RatingModel>()
                .HasOne(r => r.Story)
                .WithMany()
                .HasForeignKey(r => r.StoryId)
                .OnDelete(DeleteBehavior.NoAction);

            // -------------------------------------------------------
            // CHAPTER VIEW
            // -------------------------------------------------------
            modelBuilder.Entity<ChapterViewModel>()
                .HasOne(cv => cv.Chapter)           // ← THÊM DÒNG NÀY
                .WithMany()
                .HasForeignKey(cv => cv.ChapterId)
                .OnDelete(DeleteBehavior.NoAction); // NULL = khách vãng lai

            modelBuilder.Entity<ChapterViewModel>()
                .HasOne(cv => cv.Story)
                .WithMany()
                .HasForeignKey(cv => cv.StoryId)
                .OnDelete(DeleteBehavior.NoAction);

            // -------------------------------------------------------
            // READING HISTORY
            // -------------------------------------------------------
            modelBuilder.Entity<ReadingHistoryModel>()
                .HasOne(rh => rh.User)
                .WithMany()
                .HasForeignKey(rh => rh.UserId)
                .OnDelete(DeleteBehavior.NoAction); // ← FIX

            modelBuilder.Entity<ReadingHistoryModel>()
                .HasOne(rh => rh.Story)
                .WithMany()
                .HasForeignKey(rh => rh.StoryId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ReadingHistoryModel>()
                .HasOne(rh => rh.Chapter)
                .WithMany()
                .HasForeignKey(rh => rh.ChapterId)
                .OnDelete(DeleteBehavior.SetNull);

            // -------------------------------------------------------
            // STORY COMMENT
            // -------------------------------------------------------
            modelBuilder.Entity<StoryCommentModel>()
                .HasOne(sc => sc.User)
                .WithMany()
                .HasForeignKey(sc => sc.UserId)
                .OnDelete(DeleteBehavior.NoAction); // ← FIX

            modelBuilder.Entity<StoryCommentModel>()
                .HasOne(sc => sc.Story)
                .WithMany(s => s.StoryComments)
                .HasForeignKey(sc => sc.StoryId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa truyện → xóa comment

            modelBuilder.Entity<StoryCommentModel>()
                .HasOne(sc => sc.Parent)
                .WithMany(sc => sc.Replies)
                .HasForeignKey(sc => sc.ParentId)
                .OnDelete(DeleteBehavior.NoAction); // Self-reference phải NoAction

            // -------------------------------------------------------
            // CHAPTER COMMENT
            // -------------------------------------------------------
            modelBuilder.Entity<ChapterCommentModel>()
                .HasOne(cc => cc.User)
                .WithMany()
                .HasForeignKey(cc => cc.UserId)
                .OnDelete(DeleteBehavior.SetNull); // ← FIX

            modelBuilder.Entity<ChapterCommentModel>()
                .HasOne(cc => cc.Chapter)
                .WithMany()
                .HasForeignKey(cc => cc.ChapterId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChapterCommentModel>()
                .HasOne(cc => cc.Story)
                .WithMany()
                .HasForeignKey(cc => cc.StoryId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ChapterCommentModel>()
                .HasOne(cc => cc.Parent)
                .WithMany(cc => cc.Replies)
                .HasForeignKey(cc => cc.ParentId)
                .OnDelete(DeleteBehavior.NoAction); // Self-reference phải NoAction
                                                    // -------------------------------------------------------
                                                    //thảo luận phần tránh multi delete cho user và categor chút đẩy lên 
            modelBuilder.Entity<BaiVietModel>()
                    .HasOne(b => b.ForumCategory)
                    .WithMany(fc => fc.Threads)
                    .HasForeignKey(b => b.ForumCategoryId)
                    .OnDelete(DeleteBehavior.Cascade);

            // USER → THREAD: NoAction để tránh cycle
            modelBuilder.Entity<BaiVietModel>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // -------------------------------------------------------
            // REPLY (TraLoiBaiViet)
            // Xóa bài viết → xóa luôn reply
            // -------------------------------------------------------
            modelBuilder.Entity<TraLoiBaiVietModel>()
                .HasOne(r => r.Thread)
                .WithMany(b => b.Replies)
                .HasForeignKey(r => r.ThreadId)
                .OnDelete(DeleteBehavior.Cascade);

            // USER → REPLY: NoAction để tránh cycle
            modelBuilder.Entity<TraLoiBaiVietModel>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.NoAction);
            // -------------------------------------------------------
            // REPLY LIKE
            // -------------------------------------------------------
            modelBuilder.Entity<LikeModel>()
                .HasOne(rl => rl.Replies)
                .WithMany()
                .HasForeignKey(rl => rl.ReplyId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa reply → xóa like theo

            modelBuilder.Entity<LikeModel>()
                .HasOne(rl => rl.User)
                .WithMany(u => u.Likes)
                .HasForeignKey(rl => rl.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // -------------------------------------------------------
            // THONG BAO
            // -------------------------------------------------------
            modelBuilder.Entity<ThongBaoModel>()
                .HasOne(t => t.User)
                .WithMany(u => u.ThongBaos)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa user → xóa thông báo

            modelBuilder.Entity<ThongBaoModel>()
                .HasOne(t => t.Sender)
                .WithMany(u => u.ThongBaoGuiDi)
                .HasForeignKey(t => t.SenderId)
                .OnDelete(DeleteBehavior.NoAction); // NoAction tránh cycle
        }
    }

}
