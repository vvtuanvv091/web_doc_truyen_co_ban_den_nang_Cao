using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_doc_truyen_Co_ban.Models
{
    public class ThongBaoModel
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Người nhận thông báo
        [Required]
        public Guid UserId { get; set; }

        // Người thực hiện hành động
        public Guid? SenderId { get; set; }

        // "reply" | "like" | "follow"
        [Required]
        [MaxLength(20)]
        public string Type { get; set; } = string.Empty;

        // "đã bình luận vào bài viết của bạn: ..."
        [Required]
        [MaxLength(300)]
        public string Message { get; set; } = string.Empty;

        // Link điều hướng khi bấm vào thông báo
        [MaxLength(500)]
        public string? Link { get; set; }

        public bool IsRead { get; set; } = false;

        [MaxLength(50)]
        public string? RoleTarget { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        [InverseProperty("ThongBaos")]
        public UsersModel? User { get; set; }

        [ForeignKey("SenderId")]
        [InverseProperty("ThongBaoGuiDi")]
        public UsersModel? Sender { get; set; }
    }
}