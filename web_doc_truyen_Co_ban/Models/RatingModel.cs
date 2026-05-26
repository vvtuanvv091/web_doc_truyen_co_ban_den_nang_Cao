using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_doc_truyen_Co_ban.Models
{
    //đánh giá max 1-5 sao
    public class RatingModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid StoryId { get; set; }

        [Required]
        [Range(1, 5)]                        // Validation: chỉ cho phép 1-5
        public byte Score { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ---- Navigation Properties ----
        [ForeignKey("UserId")]
        public UsersModel User { get; set; }

        [ForeignKey("StoryId")]
        public StoryModel Story { get; set; }

        public byte Status { get; set; } = 0;  // 0 = hiển thị, 1 = đã ẩn
    }
}
