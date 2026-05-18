using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_doc_truyen_Co_ban.Models
{
    public class MembershipPlan
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty; // "Hội viên Bạc", "Vàng", "Kim Cương"

        [MaxLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; } 
        public double CoinPrice { get; set; }

        public int DurationDays { get; set; }

        public bool IsActive { get; set; } = true;

        public int ChapterUnlockPerDay { get; set; } = 0;
        public bool NoAds { get; set; } = true;
        public bool EarlyAccess { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
    public class UserMembership
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public UsersModel? User { get; set; }

        public int MembershipPlanId { get; set; }

        [ForeignKey(nameof(MembershipPlanId))]
        public MembershipPlan? Plan { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive => DateTime.UtcNow >= StartDate && DateTime.UtcNow <= EndDate;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
    public class MembershipOrder
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string OrderCode { get; set; } = string.Empty; 

        [Required]
        public Guid UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public UsersModel? User { get; set; }

        public int MembershipPlanId { get; set; }

        [ForeignKey(nameof(MembershipPlanId))]
        public MembershipPlan? Plan { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [MaxLength(200)]
        public string? QrCodeUrl { get; set; } 

        [MaxLength(200)]
        public string? QrCodeData { get; set; } 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }
        public DateTime ExpiredAt { get; set; } 

        [MaxLength(500)]
        public string? Note { get; set; }
    }

    public enum OrderStatus
    {
        Pending = 0, 
        Paid = 1,      
        Expired = 2,  
        Cancelled = 3,
        Failed = 4   
    }

    // ViewModel cho trang đăng ký hội viên
    public class MembershipViewModel
    {
        public List<MembershipPlan> Plans { get; set; } = new();
        public UserMembership? CurrentMembership { get; set; }
        public bool HasActiveMembership => CurrentMembership?.IsActive ?? false;
    }

    // ViewModel thanh toán QR
    public class QrPaymentViewModel
    {
        public MembershipOrder Order { get; set; } = null!;
        public MembershipPlan Plan { get; set; } = null!;
        public string QrImageUrl { get; set; } = string.Empty;
        public int CountdownSeconds { get; set; } = 600; // 10 phút
    }

    // Request tạo đơn hàng
    public class CreateOrderRequest
    {
        [Required]
        public int PlanId { get; set; }
    }

    // Response kiểm tra trạng thái thanh toán (dùng cho AJAX polling)
    public class PaymentStatusResponse
    {
        public string OrderCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsPaid { get; set; }
        public string? Message { get; set; }
    }
}