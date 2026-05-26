using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_doc_truyen_Co_ban.Models
{
    //Xem Luot Xem
    public class ChapterViewModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ChapterId { get; set; }

        [Required]
        public Guid StoryId { get; set; }    // Denorm: tránh join khi tổng hợp

        public Guid? UserId { get; set; }    // NULL = khách vãng lai

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

        // ---- Navigation Properties ----
        [ForeignKey("ChapterId")]
        public ChapterModel Chapter { get; set; }

        [ForeignKey("StoryId")]
        public StoryModel Story { get; set; }

        [ForeignKey("UserId")]
        public UsersModel? User { get; set; }
    }
}
