using System.ComponentModel.DataAnnotations;

namespace web_doc_truyen_Co_ban.Models
{
    public class TagModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        [Required]
        [MaxLength(70)]
        public string Slug { get; set; }

        public int StoryCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ---- Navigation Properties ----
        // Many-to-Many với Stories qua bảng StoryTags
        public ICollection<StoryModel> Stories { get; set; } = new List<StoryModel>();
    }
}
