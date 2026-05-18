using System.ComponentModel.DataAnnotations;

namespace web_doc_truyen_Co_ban.Models
{
    public class RolesModel
    {
        [Key]                               
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        public string? Permissions { get; set; }  // Lưu dạng JSON string quyền ch

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ---- Navigation Properties (quan hệ 1-nhiều) ----
        public ICollection<UsersModel> Users { get; set; } = new List<UsersModel>();
    }
}
