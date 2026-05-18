using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_doc_truyen_Co_ban.Models
{
    public class TraLoiBaiVietModel
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ThreadId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string Content { get; set; }

        public int LikeCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("ThreadId")]
        public BaiVietModel? Thread { get; set; }

        [ForeignKey("UserId")]
        public UsersModel? User { get; set; }
    }
}
