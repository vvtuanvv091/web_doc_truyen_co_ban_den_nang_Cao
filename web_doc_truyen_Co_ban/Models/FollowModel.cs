using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_doc_truyen_Co_ban.Models
{
    public class FollowModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid StoryId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ---- Navigation Properties ----
        [ForeignKey("UserId")]
        public UsersModel User { get; set; }

        [ForeignKey("StoryId")]
        public StoryModel Story { get; set; }
        public byte? Status { get; set; } = 0; // 0 và 1 ẩn
    }
}
