using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_doc_truyen_Co_ban.Models
{
    public class UsersSessionModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(500)]
        public string RefreshToken { get; set; } //cấp lại token

        [MaxLength(45)]
        public string? IpAddress { get; set; }// Lưu địa chỉ IP của thiết bị đăng nhập

        [MaxLength(500)]
        public string? UserAgent { get; set; }// Lưu thông tin trình duyệt/thiết bị

        [Required]
        public DateTime ExpiresAt { get; set; }// thời điểm token hết hiệu lức

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; //thời điểm tạo lấy thời gina hiện tại

        // ---- Navigation Properties ----
        [ForeignKey("UserId")]
        public UsersModel User { get; set; }

    }
    [NotMapped]
    public class UserSessionViewModel
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string IpAddress { get; set; } = "";
        public string? UserAgent { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsExpired { get; set; }
        public DeviceInfoViewModel DeviceInfo { get; set; } = new();
    }
    [NotMapped]
    public class DeviceInfoViewModel
    {
        public string Browser { get; set; } = "—";
        public string Os { get; set; } = "—";
        public string DeviceFamily { get; set; } = "—";
        public string DeviceType { get; set; } = "desktop"; // mobile | tablet | desktop | bot
        public string Raw { get; set; } = "—";
    }
}
