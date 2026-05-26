using System.ComponentModel.DataAnnotations;

namespace web_doc_truyen_Co_ban.Models
{
    public class CategoryModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [MaxLength(120)]
        public string Slug { get; set; }     // URL-friendly: "tien-hiep"

        public string? Description { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public int StoryCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ---- Navigation Properties ----
        public ICollection<StoryModel> Stories { get; set; } = new List<StoryModel>();
    }
}
