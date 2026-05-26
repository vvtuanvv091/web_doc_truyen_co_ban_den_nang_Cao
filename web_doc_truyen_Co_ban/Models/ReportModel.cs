using DocumentFormat.OpenXml.Office2013.PowerPoint.Roaming;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_doc_truyen_Co_ban.Models
{
    public class ReportModel
    {
        [Key]
        public Guid BaocaoId { get; set; }
        [Required]
        public Guid UserId { get; set; }

        // Báo cáo cái gì: "comment" | "truyen" | "chuong" | "user"
        [Required]
        [MaxLength(20)]
        public string TargetType { get; set; } = string.Empty;

        [Required]
        public Guid TargetId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;//lysy do là j
        [MaxLength(20)]
        public string Status { get; set; } = "pending";//

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;//ngày gửi

        public DateTime? ProcessedAt { get; set; }//xửlys

        [ForeignKey("UserId")]
        [InverseProperty("ReportDaGui")]
        public UsersModel? User { get; set; }
    }
}
