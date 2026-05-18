using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_doc_truyen_Co_ban.Models
{
    public class ChapterCommentModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ChapterId { get; set; }

        [Required]
        public Guid StoryId { get; set; }    // Denorm: query tổng comment của truyện

        [Required]
        public Guid UserId { get; set; }

        public Guid? ParentId { get; set; }

        [Required]
        [MaxLength(2000)]
        [MinLength(1)]
        public string Content { get; set; }

        public int LikeCount { get; set; } = 0;
        public byte Status { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ---- Navigation Properties ----
        [ForeignKey("ChapterId")]
        public ChapterModel Chapter { get; set; }

        [ForeignKey("StoryId")]
        public StoryModel Story { get; set; }

        [ForeignKey("UserId")]
        public UsersModel User { get; set; }

        [ForeignKey("ParentId")]
        public ChapterCommentModel? Parent { get; set; }

        public ICollection<ChapterCommentModel> Replies { get; set; } = new List<ChapterCommentModel>();
    }
}
