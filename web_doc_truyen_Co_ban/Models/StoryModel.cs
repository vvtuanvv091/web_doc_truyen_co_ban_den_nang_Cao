using Azure;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_doc_truyen_Co_ban.Models
{
    public class StoryModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Title { get; set; }

        [Required]
        [MaxLength(600)]
        public string Slug { get; set; }

        //[Required]
        public Guid? AuthorId { get; set; }   // FK tới Users

        [Required]
        public Guid CategoryId { get; set; } // FK tới Categories

        public string? Description { get; set; }

        [MaxLength(500)]
        public string? CoverImageUrl { get; set; }

        [MaxLength(200)]
        public string? Source { get; set; }  // Nguồn dịch

        [MaxLength(200)]
        public string? OriginalAuthor { get; set; }

        // Status: 0=Bản nháp, 1=Đang ra, 2=Hoàn thành, 3=Tạm dừng, 4=Drop
        public byte Status { get; set; } = 1;

        // Thống kê denormalized
        public int TotalChapters { get; set; } = 0;
        public long TotalViews { get; set; } = 0;
        public int TotalFollows { get; set; } = 0;
        public int TotalComments { get; set; } = 0;
        public long RatingSum { get; set; } = 0;
        public int RatingCount { get; set; } = 0;

        [Column(TypeName = "decimal(3,2)")]  // Phải khai báo rõ kiểu decimal
        public decimal RatingAvg { get; set; } = 0;

        // Flags
        public bool IsFeatured { get; set; } = false;//nổi bât
        public bool IsVip { get; set; } = false;//vip ưu tiên
        public bool Is18Plus { get; set; } = false; ///giới hạn tuổi

        public DateTime? LastChapterAt { get; set; } //thời điểm cuôi cùng nghĩa chương ms ra
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; /// thời điểm tạo bài viets lần đầu
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // thời điểm update

        // ---- Navigation Properties ----
        [ForeignKey("AuthorId")]
        public UsersModel? Author { get; set; }

        [ForeignKey("CategoryId")]
        public CategoryModel? Category { get; set; }

        // Many-to-Many với Tags
        public ICollection<TagModel> Tags { get; set; } = new List<TagModel>();

        public ICollection<ChapterModel> Chapters { get; set; } = new List<ChapterModel>();
        public ICollection<FollowModel> Follows { get; set; } = new List<FollowModel>();
        public ICollection<BookmarkModel> Bookmarks { get; set; } = new List<BookmarkModel>();
        public ICollection<RatingModel> Ratings { get; set; } = new List<RatingModel>();
        [InverseProperty("Story")]
        public ICollection<StoryCommentModel> StoryComments { get; set; } = new List<StoryCommentModel>();
    }
}
