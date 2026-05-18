using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_doc_truyen_Co_ban.Models
{
    //tiến độ đọc
    public class BookmarkModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid StoryId { get; set; }

        public Guid? ChapterId { get; set; }  // nullable: chương đang đọc

        [Column(TypeName = "decimal(10,2)")]
        public decimal? ChapterNumber { get; set; }  // snapshot để hiển thị nhanh

        // % vị trí cuộn trang (0-100)
        [Range(0, 100)]
        public int ScrollPosition { get; set; } = 0;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ---- Navigation Properties ----
        [ForeignKey("UserId")]
        public UsersModel User { get; set; }

        [ForeignKey("StoryId")]
        public StoryModel Story { get; set; }

        [ForeignKey("ChapterId")]
        public ChapterModel? Chapter { get; set; }
    }
}
