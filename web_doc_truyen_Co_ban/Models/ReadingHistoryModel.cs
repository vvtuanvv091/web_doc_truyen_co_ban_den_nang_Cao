using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_doc_truyen_Co_ban.Models
{
    public class ReadingHistoryModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid StoryId { get; set; }

        public Guid? ChapterId { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? ChapterNumber { get; set; }

        public DateTime LastReadAt { get; set; } = DateTime.UtcNow;

        // ---- Navigation Properties ----
        [ForeignKey("UserId")]
        public UsersModel User { get; set; }

        [ForeignKey("StoryId")]
        public StoryModel Story { get; set; }

        [ForeignKey("ChapterId")]
        public ChapterModel? Chapter { get; set; }
    }
}
