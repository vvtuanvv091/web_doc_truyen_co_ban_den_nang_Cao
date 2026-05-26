namespace web_doc_truyen_Co_ban.Services
{
    /// <summary>
    /// Service tạo QR chuyển khoản ngân hàng theo chuẩn VietQR (Napas 247)
    /// Không cần tích hợp cổng thanh toán — dùng chuyển khoản thường
    public class VietQrService
    {
        // === CẤU HÌNH NGÂN HÀNG CỦA BẠN ===
        // Thay bằng thông tin tài khoản thật của bạn
        private readonly string _bankId;         // Mã ngân hàng VietQR, ví dụ: "970422" (MB Bank)
        private readonly string _accountNo;      // Số tài khoản
        private readonly string _accountName;    // Tên chủ tài khoản (VIẾT HOA, không dấu)
        private readonly string _template;       // "compact" hoặc "qr_only" hoặc "print"

        public VietQrService(IConfiguration config)
        {
            _bankId = config["VietQR:BankId"] ?? "970422";
            _accountNo = config["VietQR:AccountNo"] ?? "0984391940";
            _accountName = config["VietQR:AccountName"] ?? "TONG NGOC TUAN";
            _template = config["VietQR:Template"] ?? "compact";
        }
        public string GetQrImageUrl(long amount, string description)
        {
            // VietQR Quick Link API — hoàn toàn miễn phí
            // Tài liệu: https://www.vietqr.io/danh-sach-api/api-tao-ma-qr/
            var encodedDesc = Uri.EscapeDataString(description);
            var encodedName = Uri.EscapeDataString(_accountName);

            return $"https://img.vietqr.io/image/{_bankId}-{_accountNo}-{_template}.png" +
                   $"?amount={amount}" +
                   $"&addInfo={encodedDesc}" +
                   $"&accountName={encodedName}";
        }
        public string GenerateQrData(long amount, string description, string orderCode)
        {
            // Đơn giản: trả về URL VietQR (cũng là QR data hợp lệ)
            return GetQrImageUrl(amount, description);
        }
        public string GetQrImageUrl(string amount, string description)
            => GetQrImageUrl(long.Parse(amount), description);
    }
}

 //* MB Bank:      970422
 //* Vietcombank:  970436
 //* Techcombank:  970407
 //* VPBank:       970432
 //* ACB:          970416
 //* Agribank:     970405
 //* BIDV:         970418
 //* Vietinbank:   970415
 //* TPBank:       970423