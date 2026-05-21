using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_doc_truyen_Co_ban.Models
{
    public class StoryCommentModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid StoryId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        public Guid? ParentId { get; set; } // NULL = comment gốc, có giá trị = reply

        [Required]
        [MaxLength(2000)]
        [MinLength(1)]
        public string Content { get; set; }

        public int LikeCount { get; set; } = 0;

        // Status: 0=Hiển thị, 1=Ẩn (mod), 2=Đã xóa
        public byte Status { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ---- Navigation Properties ----
        [ForeignKey("StoryId")]
        public StoryModel Story { get; set; }

        [ForeignKey("UserId")]
        public UsersModel User { get; set; }

        // Self-reference: comment cha
        [ForeignKey("ParentId")]
        public StoryCommentModel? Parent { get; set; }

        // Danh sách reply con
        [InverseProperty("Parent")]
        public ICollection<StoryCommentModel> Replies { get; set; } = new List<StoryCommentModel>();

        [NotMapped]
        public string? StoryTitle { get; set; }

        [NotMapped]
        public string? UserName { get; set; }

        [NotMapped]
        public int ReplyCount { get; set; }
    }
}
