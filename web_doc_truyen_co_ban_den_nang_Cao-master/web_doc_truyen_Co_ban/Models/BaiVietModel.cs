using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_doc_truyen_Co_ban.Models
{
    public class BaiVietModel
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ForumCategoryId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        public int ViewCount { get; set; } = 0;

        public int ReplyCount { get; set; } = 0;

        public bool IsPinned { get; set; } = false;

        public bool IsLocked { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("ForumCategoryId")]
        public ThaoLuanDanhMucModel? ForumCategory { get; set; }

        [ForeignKey("UserId")]
        public UsersModel? User { get; set; }

        public ICollection<TraLoiBaiVietModel> Replies { get; set; }
            = new List<TraLoiBaiVietModel>();
    }
}
