using System;
using System.Collections.Generic;

namespace web_doc_truyen_Co_ban.Models
{
    public class DashBoardModel
    {
        // 4 Thẻ tổng số lượng
        public int TongTruyen { get; set; }
        public int TongChuong { get; set; }
        public int TongUser { get; set; }
        public decimal TongDonate { get; set; }

        // Thống kê tương tác
        public int TongBinhLuan { get; set; }
        public int TongDanhGia { get; set; }
        public int TongTheoDoi { get; set; }
        public int TongBookmark { get; set; }

        // Danh sách hiển thị bảng dữ liệu
        public List<TopStoryItem> TopTruyen { get; set; } = new();
        public List<TopDonateItem> TopDonate { get; set; } = new();
        public List<RatingModel> TopRatingTruyen { get; set; }
        public List<RecentTransactionItem> RecentTransactions { get; set; } = new();

        // Cập nhật: Thêm 2 thuộc tính bị thiếu để Controller không bị lỗi compile
        public List<RatingDistributionItem> RatingDistribution { get; set; } = new();
        public List<TopRatedStoryItem> TopRatedS { get; set; } = new();
    }

    public class TopStoryItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public int TotalViews { get; set; }
        public string Status { get; set; } = "";
    }

    public class TopDonateItem
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = "";
        public long TongXu { get; set; }
    }

    public class RecentTransactionItem
    {
        public string Username { get; set; } = "";
        public long Amount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Type { get; set; } = "";
    }

    public class RatingDistributionItem
    {
        public int Star { get; set; }
        public int Count { get; set; }
        public int Percent { get; set; }
    }

    public class TopRatedStoryItem
    {
        public Guid StoryId { get; set; }
        public string Title { get; set; } = "";
        public double AvgScore { get; set; }
        public int RatingCount { get; set; }
    }
}