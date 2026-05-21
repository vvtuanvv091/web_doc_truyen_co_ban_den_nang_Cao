using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_doc_truyen_Co_ban.Models
{
    public class UnlockedChapter
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid ChapterId { get; set; }

        [ForeignKey(nameof(UserId))]
        public UsersModel? User { get; set; }

        [ForeignKey(nameof(ChapterId))]
        public ChapterModel? Chapter { get; set; }

        public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;
    }
}
