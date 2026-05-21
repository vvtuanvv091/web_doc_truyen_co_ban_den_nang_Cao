using System.ComponentModel.DataAnnotations;

namespace web_doc_truyen_Co_ban.Models
{
    public class ThaoLuanDanhMucModel
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [Required]
        [MaxLength(200)]
        public string Slug { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<BaiVietModel> Threads { get; set; }
            = new List<BaiVietModel>();
    }
}
