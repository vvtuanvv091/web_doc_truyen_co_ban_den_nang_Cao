using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_doc_truyen_Co_ban.Models
{
    public class CoinTransaction
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public UsersModel? User { get; set; }
        public long Amount { get; set; }
        public long BalanceAfter { get; set; }

        public CoinTransactionType Type { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum CoinTransactionType
    {
        Recharge = 0,//nap xu
        UnlockChapter = 1,//mo khoa chung
        BuyMembership = 2,//mua hoi vien
        MembershipBonus = 3,//hoan xu hoi vien
        Refund = 4,//hoan xu khi huy don hang
        AdminAdd = 5
    }

}
