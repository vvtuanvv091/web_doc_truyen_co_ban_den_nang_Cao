using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace web_doc_truyen_Co_ban.Models
{
    public class UsersModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        [MaxLength(255)]
        [EmailAddress]                       
        public string Email { get; set; }

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; }  

        [MaxLength(100)]
        public string? DisplayName { get; set; }

        [MaxLength(500)]
        public string? AvatarUrl { get; set; }

        public string? Bio { get; set; }  

        // FK tới Roles (nullable vì có thể chưa gán role)
        public Guid? RoleId { get; set; }

        // Status: 0=Chưa xác minh, 1=Hoạt động, 2=Tạm khóa, 3=Cấm vĩnh viễn
        public byte Status { get; set; } = 0;

        public long coins { get; set; } = 0;

        public bool EmailVerified { get; set; } = false;

        [MaxLength(100)]
        public string? VerifyToken { get; set; }

        [MaxLength(100)]
        public string? ResetToken { get; set; }

   
        public DateTime? ResetExpires { get; set; }

        // Thống kê denormalized (cập nhật bởi trigger hoặc service layer)
        public int TotalFollows { get; set; } = 0;
        public int TotalStories { get; set; } = 0;

        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [ForeignKey("RoleId")] 
        public RolesModel? Role { get; set; }//1=> nhiều

        public ICollection<UsersSessionModel> Sessions { get; set; } = new List<UsersSessionModel>(); //nhiều => 1
        public ICollection<StoryModel> Stories { get; set; } = new List<StoryModel>();
        public ICollection<FollowModel> Follows { get; set; } = new List<FollowModel>();
        public ICollection<BookmarkModel> Bookmarks { get; set; } = new List<BookmarkModel>();
        public ICollection<RatingModel> Ratings { get; set; } = new List<RatingModel>();

        public ICollection<ThongBaoModel> ThongBaos { get; set; } = new List<ThongBaoModel>();         // thông báo nhận
        public ICollection<ThongBaoModel> ThongBaoGuiDi { get; set; } = new List<ThongBaoModel>();     // thông báo đã gửi
        public ICollection<LikeModel> Likes { get; set; } = new List<LikeModel>();       // các like đã bấm
    }
}
