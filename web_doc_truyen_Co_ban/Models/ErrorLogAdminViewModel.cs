using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_doc_truyen_Co_ban.Models
{
    public class ErrorLogAdminViewModel
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid? UserId { get; set; }// nullable vì có thể user chưa đăng nhập
        // Hành động cụ thể:
        // Read | Bookmark | UnBookmark | Comment | DeleteComment đã hoàn thành
        // Follow | UnFollow | Rating | BuyChapter | BuyCoin
        // Login | Logout | Register | UpdateProfile | ChangePassword
        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        // ── TRÊN MODEL NÀO ──────────────────────────────────
        // Tên model/bảng bị tác động:
        [MaxLength(50)]
        public string? EntityType { get; set; }
        // ID của bản ghi 
        public Guid? EntityId { get; set; }

        // ── KẾT QUẢ ─────────────────────────────────────────
        public bool IsSuccess { get; set; } = true;

        // Nếu thất bại thì lý do là gì
        [MaxLength(500)]
        public string? ErrorMessage { get; set; }

        // ── THÔNG TIN THIẾT BỊ ──────────────────────────────
        [MaxLength(45)]
        public string? IpAddress { get; set; }

        // Trình duyệt / thiết bị (VD: Chrome, Firefox, Mobile...)
        [MaxLength(500)]
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public UsersModel? User { get; set; }
    }
}
