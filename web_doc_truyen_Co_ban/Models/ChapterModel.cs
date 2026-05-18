using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_doc_truyen_Co_ban.Models
{
    public class ChapterModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid StoryId { get; set; }

        // Dùng decimal để hỗ trợ chương 1.5, 2.5 (extra chapter)
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal ChapterNumber { get; set; }
        [MaxLength(200)]
        public string? VolumeTitle { get; set; }  

        [MaxLength(500)]
        public string? Title { get; set; }

        public string? Content { get; set; } // Nội dung chương

        public int WordCount { get; set; } = 0;

        // Status: 0=Bản nháp, 1=Đã đăng, 2=Khoá (VIP), 3=Đã xóa
        public byte Status { get; set; } = 0;

        public bool IsLocked { get; set; } = false;
        public int UnlockPrice { get; set; } = 0;  // Số xu để mở khoá

        public long ViewCount { get; set; } = 0;

        public DateTime? PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ---- Navigation Properties ----
        [ForeignKey("StoryId")]
        public StoryModel? Story { get; set; }

        public ICollection<ChapterCommentModel> ChapterComments { get; set; } = new List<ChapterCommentModel>();
        public ICollection<ChapterViewModel> ChapterViews { get; set; } = new List<ChapterViewModel>();
    }
}
