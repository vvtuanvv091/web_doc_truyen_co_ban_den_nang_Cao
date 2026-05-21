using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Models;
using web_doc_truyen_Co_ban.Services;

namespace web_doc_truyen_Co_ban.Controllers
{
    public class MembershipPlanController : Controller
    {
        private readonly ApplicationDbContext _dbcontext;
        private readonly ILogger<MembershipPlanController> _logger;
        private readonly VietQrService _vietQrService;
        private readonly IConfiguration _config;
        public MembershipPlanController(ApplicationDbContext db, ILogger<MembershipPlanController> logger, VietQrService vietQrService, IConfiguration config)
        {
            _dbcontext = db;
            _logger = logger;
            _vietQrService = vietQrService;
            _config = config;
        }

        private Guid GetUserId()
        {
            var userId=User.FindFirstValue(ClaimTypes.NameIdentifier)??string.Empty;
            return Guid.TryParse(userId, out var guid) ? guid : Guid.Empty;
        }
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();

            var plans = await _dbcontext.MembershipPlans
                .Where(p => p.IsActive)
                .OrderBy(p => p.Price)
                .ToListAsync();//cac goi hoi vien dang hoat dong, sap xep theo gia tang dan
            var currentPlan = await _dbcontext.UserMemberships
                .Where(um => um.UserId == userId && um.EndDate > DateTime.UtcNow)
                .OrderByDescending(um => um.EndDate)
                .FirstOrDefaultAsync(); //cap nhat goi hoi vien hien tai cua nguoi dung (neu co)
            var viewmodel = new MembershipViewModel
            {
                Plans = plans,
                CurrentMembership = currentPlan
            };//tạo thùng trung gian lý do xử lý riêng gọi hội viên và gói hội viên, tránh lộn xộn trong view


            return View(viewmodel);
        }
        //mymemnbership
        [HttpGet]
        public async Task<IActionResult> MyMemberShíp()
        {

            var userId = GetUserId();
            var memberships = await _dbcontext.UserMemberships
                .Include(m => m.Plan)
                .Where(m => m.UserId == userId && m.EndDate > DateTime.UtcNow)
                .OrderByDescending(m => m.EndDate)
                .FirstOrDefaultAsync();
            var orders = await _dbcontext.MembershipOrders
                .Include(o => o.Plan)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .ToListAsync();
            ViewBag.Orders = orders;
            return View(memberships);
        }

        //private async Task ActivateMembershipAsync(Guid userId, MembershipPlan plan)
        //{
        //    var existing = await _dbcontext.UserMemberships
        //        .Where(m => m.UserId == userId && m.EndDate >= DateTime.UtcNow)
        //        .OrderByDescending(m => m.EndDate)
        //        .FirstOrDefaultAsync();

        //    if (existing != null && existing.MembershipPlanId == plan.Id)
        //    {
        //        // Cùng gói → cộng thêm ngày
        //        existing.EndDate = existing.EndDate.AddDays(plan.DurationDays);
        //    }
        //    else
        //    {
        //        // Gói mới / nâng cấp → tạo bản ghi mới
        //        var now = DateTime.UtcNow;
        //        _dbcontext.UserMemberships.Add(new UserMembership
        //        {
        //            UserId = userId,
        //            MembershipPlanId = plan.Id,
        //            StartDate = now,
        //            EndDate = now.AddDays(plan.DurationDays)
        //        });
        //    }
        //}
        private async Task ActivateMembershipAsync(Guid userId, MembershipPlan plan)
        {
            // ── Cộng xu vào tài khoản ──
            var user = await _dbcontext.Users.FindAsync(userId);
            if (user != null)
                user.coins += (long)plan.CoinPrice; // CoinPrice trong MembershipPlan

            // ── Tạo/gia hạn membership như cũ ──
            var existing = await _dbcontext.UserMemberships
                .Where(m => m.UserId == userId && m.EndDate >= DateTime.UtcNow)
                .OrderByDescending(m => m.EndDate)
                .FirstOrDefaultAsync();

            if (existing != null && existing.MembershipPlanId == plan.Id)
            {
                existing.EndDate = existing.EndDate.AddDays(plan.DurationDays);
            }
            else
            {
                var now = DateTime.UtcNow;
                _dbcontext.UserMemberships.Add(new UserMembership
                {
                    UserId = userId,
                    MembershipPlanId = plan.Id,
                    StartDate = now,
                    EndDate = now.AddDays(plan.DurationDays)
                });
            }
        }

        private static string GenerateOrderCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var rng = new Random();
            var rand = new string(Enumerable.Range(0, 8)
                .Select(_ => chars[rng.Next(chars.Length)]).ToArray());
            return $"{DateTime.UtcNow:yyMMdd}{rand}";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrder([FromForm] CreateOrderRequest requeest)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }
            var userId = GetUserId();
            //var plan=await _dbcontext.MembershipPlans.FirstOrDefaultAsync(p=>p.Id==requeest.PlanId && p.IsActive);
            var plans = await _dbcontext.MembershipPlans.FindAsync(requeest.PlanId);
            if (plans == null || !plans.IsActive)
            {
                TempData["Error"]="Gói hội viên không tồn tại hoặc không hoạt động";
                return RedirectToAction("Index");
            }    
            //gói miễn phí
            if(plans.Price == 0)
            {
                await ActivateMembershipAsync(userId, plans);
                await _dbcontext.SaveChangesAsync();
                ViewData["Success"]="Kích hoạt gói hội viên miễn phí thành công!";
                return RedirectToAction("Index");
            }    
            //đơn peding
            var existingPending = await _dbcontext.MembershipOrders
                .Where(u => u.UserId == userId
                && u.MembershipPlanId == plans.Id
                && u.Status == OrderStatus.Pending
                &&u.ExpiredAt> DateTime.UtcNow
                )
                .FirstOrDefaultAsync();
            if (existingPending != null)
                return RedirectToAction("Payment", new { orderCode = existingPending.OrderCode });
            // Tạo mã đơn mới — SePay khớp nội dung CK theo mã này
            var orderCode   = GenerateOrderCode();
            var description = $"HOIVIEN {orderCode}";
 
            var order = new MembershipOrder
            {
                OrderCode        = orderCode,
                UserId           = userId,
                MembershipPlanId = plans.Id,
                Amount           = plans.Price,
                Status           = OrderStatus.Pending,
                ExpiredAt        = DateTime.UtcNow.AddMinutes(15),
                QrCodeUrl        = _vietQrService.GetQrImageUrl((long)plans.Price, description),
                QrCodeData       = description,
                Note             = $"Thanh toán gói {plans.Name}"
            };
 
            _dbcontext.MembershipOrders.Add(order);
            await _dbcontext.SaveChangesAsync();
 
            return RedirectToAction("Payment", new { orderCode });

        }

        //payment trang thai va xac nhan
        [HttpGet]
        public async Task<IActionResult> Success(string orderCode)
        {
            var userId = GetUserId();

            var order = await _dbcontext.MembershipOrders
                .Include(o => o.Plan)
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode
                                       && o.UserId == userId
                                       && o.Status == OrderStatus.Paid);

            if (order == null) return RedirectToAction(nameof(Index));

            return View(order);
        }
        // GET /Membership/CheckStatus?orderCode=xxx  — AJAX polling
        // Gọi mỗi 5 giây từ trang Payment.cshtml để biết đã CK chưa
        [HttpGet]
        public async Task<IActionResult> CheckStatus(string orderCode)
        {
            var userId = GetUserId();

            var order = await _dbcontext.MembershipOrders
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode && o.UserId == userId);

            if (order == null)
                return Json(new PaymentStatusResponse
                {
                    OrderCode = orderCode,
                    Status = "NotFound",
                    IsPaid = false,
                    Message = "Không tìm thấy đơn hàng."
                });

            // Tự expire
            if (order.Status == OrderStatus.Pending && order.ExpiredAt < DateTime.UtcNow)
            {
                order.Status = OrderStatus.Expired;
                await _dbcontext.SaveChangesAsync();
            }

            return Json(new PaymentStatusResponse
            {
                OrderCode = order.OrderCode,
                Status = order.Status.ToString(),
                IsPaid = order.Status == OrderStatus.Paid,
                Message = order.Status switch
                {
                    OrderStatus.Paid => "Thanh toán thành công!",
                    OrderStatus.Expired => "Đơn hàng đã hết hạn.",
                    OrderStatus.Pending => "Đang chờ thanh toán...",
                    _ => order.Status.ToString()
                }
            });
        }

        [HttpGet]
        public async Task<IActionResult> Payment(string orderCode)
        {
            var userId = GetUserId();

            var order = await _dbcontext.MembershipOrders
                .Include(o => o.Plan)
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode && o.UserId == userId);

            if (order == null) return NotFound();

            // Đã thanh toán → chuyển sang Success
            if (order.Status == OrderStatus.Paid)
                return RedirectToAction(nameof(Success), new { orderCode });

            // Tự expire nếu quá hạn
            if (order.Status == OrderStatus.Pending && order.ExpiredAt < DateTime.UtcNow)
            {
                order.Status = OrderStatus.Expired;
                await _dbcontext.SaveChangesAsync();
            }

            var secondsLeft = (int)Math.Max(0, (order.ExpiredAt - DateTime.UtcNow).TotalSeconds);

            var vm = new QrPaymentViewModel
            {
                Order = order,
                Plan = order.Plan!,
                QrImageUrl = order.QrCodeUrl ?? string.Empty,
                CountdownSeconds = secondsLeft
            };

            return View(vm);
        }

        //ai làm
        // POST /Membership/SePayWebhook  — SePay gọi khi CK về
        // KHÔNG cần [Authorize] — SePay gọi server-to-server
        // Cấu hình trong SePay Dashboard: URL + Apikey = SecretKey
        //[AllowAnonymous]
        //[HttpPost]
        //public async Task<IActionResult> SePayWebhook()
        //{
        //    // 1. Xác thực secret key từ header
        //    var secretKey = _config["SePay:SecretKey"] ?? string.Empty;
        //    var authHeader = Request.Headers["Authorization"].FirstOrDefault() ?? string.Empty;

        //    if (authHeader != $"Apikey {secretKey}")
        //    {
        //        _logger.LogWarning("[SePay] Unauthorized – sai SecretKey. Header: {H}", authHeader);
        //        return Unauthorized(new { message = "Unauthorized" });
        //    }

        //    // 2. Đọc body JSON
        //    string body;
        //    using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        //        body = await reader.ReadToEndAsync();

        //    _logger.LogInformation("[SePay] Webhook received: {Body}", body);

        //    SePayWebhookPayload? payload;
        //    try
        //    {
        //        payload = JsonSerializer.Deserialize<SePayWebhookPayload>(body,
        //            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "[SePay] JSON parse error");
        //        return BadRequest(new { message = "Invalid JSON" });
        //    }

        //    if (payload == null)
        //        return BadRequest(new { message = "Empty payload" });

        //    // 3. Chỉ xử lý tiền VÀO
        //    if (!string.Equals(payload.TransferType, "in", StringComparison.OrdinalIgnoreCase))
        //    {
        //        _logger.LogInformation("[SePay] Bỏ qua giao dịch type={T}", payload.TransferType);
        //        return Ok(new { message = "Ignored – not a credit transaction" });
        //    }

        //    // 4. Tìm mã đơn trong nội dung CK
        //    //    Ví dụ nội dung: "HOIVIEN 26051234ABCD1234"
        //    var content = payload.Content ?? string.Empty;
        //    var orderCode = ExtractOrderCode(content);

        //    if (string.IsNullOrEmpty(orderCode))
        //    {
        //        _logger.LogWarning("[SePay] Không tìm thấy mã đơn trong: {C}", content);
        //        return Ok(new { message = "No matching order code in content" });
        //    }

        //    // 5. Tìm đơn Pending
        //    var order = await _dbcontext.MembershipOrders
        //        .Include(o => o.Plan)
        //        .FirstOrDefaultAsync(o => o.OrderCode == orderCode
        //                               && o.Status == OrderStatus.Pending);

        //    if (order == null)
        //    {
        //        _logger.LogWarning("[SePay] Không tìm thấy đơn Pending: {Code}", orderCode);
        //        return Ok(new { message = "Order not found or already processed" });
        //    }

        //    // 6. Kiểm tra số tiền (±1000đ để chống lỗi làm tròn)
        //    var paid = (decimal)payload.TransferAmount;
        //    if (Math.Abs(paid - order.Amount) > 1000)
        //    {
        //        _logger.LogWarning("[SePay] Sai số tiền. Expected={E}, Got={G}", order.Amount, paid);
        //        order.Status = OrderStatus.Failed;
        //        order.Note = $"Sai số tiền: nhận {paid}đ, yêu cầu {order.Amount}đ";
        //        await _dbcontext.SaveChangesAsync();
        //        return Ok(new { message = "Amount mismatch – order marked Failed" });
        //    }

        //    // 7. Đánh dấu Paid
        //    order.Status = OrderStatus.Paid;
        //    order.PaidAt = DateTime.UtcNow;
        //    order.Note = $"SePay ref: {payload.ReferenceCode}";

        //    // 8. Kích hoạt / gia hạn membership
        //    await ActivateMembershipAsync(order.UserId, order.Plan!);

        //    await _dbcontext.SaveChangesAsync();

        //    _logger.LogInformation("[SePay] OK – đã kích hoạt {Plan} cho user {UserId}",
        //        order.Plan!.Name, order.UserId);

        //    return Ok(new { message = "OK" });
        //}

        [AllowAnonymous]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SePayWebhook()
        {
            Request.EnableBuffering();

            string body;
            using (var reader = new StreamReader(
                Request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true))  // ← THÊM CÁI NÀY
            {
                body = await reader.ReadToEndAsync();
            }
            // Bây giờ dòng này mới an toàn
            Request.Body.Position = 0;

            _logger.LogInformation("[SePay] Webhook received: {Body}", body);
            if (string.IsNullOrWhiteSpace(body))
            {
                _logger.LogError("[SePay] Body rỗng!");
                return BadRequest(new { message = "Empty body" });
            }
            SePayWebhookPayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<SePayWebhookPayload>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SePay] JSON parse error");
                return BadRequest(new { message = "Invalid JSON" });
            }

            if (payload == null)
                return BadRequest(new { message = "Empty payload" });

            if (!string.Equals(payload.TransferType, "in", StringComparison.OrdinalIgnoreCase))
                return Ok(new { message = "Ignored" });

            var orderCode = ExtractOrderCode(payload.Content ?? string.Empty);
            if (string.IsNullOrEmpty(orderCode))
            {
                _logger.LogWarning("[SePay] Không tìm thấy mã đơn trong: {C}", payload.Content);
                return Ok(new { message = "No order code" });
            }

            var order = await _dbcontext.MembershipOrders
                .Include(o => o.Plan)
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode && o.Status == OrderStatus.Pending);

            if (order == null)
            {
                _logger.LogWarning("[SePay] Không tìm thấy đơn Pending: {Code}", orderCode);
                return Ok(new { message = "Order not found" });
            }

            var paid = (decimal)payload.TransferAmount;
            if (Math.Abs(paid - order.Amount) > 1000)
            {
                order.Status = OrderStatus.Failed;
                order.Note = $"Sai số tiền: nhận {paid}đ, yêu cầu {order.Amount}đ";
                await _dbcontext.SaveChangesAsync();
                return Ok(new { message = "Amount mismatch" });
            }

            order.Status = OrderStatus.Paid;
            order.PaidAt = DateTime.UtcNow;
            order.Note = $"SePay ref: {payload.ReferenceCode}";

            await ActivateMembershipAsync(order.UserId, order.Plan!);
            await _dbcontext.SaveChangesAsync();

            _logger.LogInformation("[SePay] OK – kích hoạt {Plan} cho user {UserId}", order.Plan!.Name, order.UserId);
            return Ok(new { message = "OK" });
        }




        // Sinh mã đơn dạng: yyMMdd + 8 ký tự random
        //Ví dụ: 26051412AB34CD56
        //private static string? ExtractOrderCode(string content)
        //{
        //    if (string.IsNullOrWhiteSpace(content)) return null;

        //    var parts = content.ToUpper().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        //    foreach (var part in parts)
        //    {
        //        if (part.Length == 14 && part[..6].All(char.IsDigit))
        //            return part;
        //    }
        //    return null;
        //}

        private static string? ExtractOrderCode(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return null;

            var parts = content.ToUpper().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                // Trim dấu đặc biệt trước khi check
                var clean = part.TrimEnd('-', '/', '.', ',');
                if (clean.Length == 14 && clean[..6].All(char.IsDigit))
                    return clean;
            }
            return null;
        }

        //lịch sử đơn hàng thông tin hội viên và ls

        [HttpGet]
        public async Task<IActionResult> MyMembership()
        {
            var userId = GetUserId();

            var membership = await _dbcontext.UserMemberships
                .Include(m => m.Plan)
                .Where(m => m.UserId == userId && m.EndDate >= DateTime.UtcNow)
                .OrderByDescending(m => m.EndDate)
                .FirstOrDefaultAsync();

            var orders = await _dbcontext.MembershipOrders
                .Include(o => o.Plan)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .ToListAsync();

            ViewBag.Orders = orders;
            return View(membership);
        }
        // POST /Membership/CancelPending  — Huỷ đơn đang chờ═
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelPending(string orderCode)
        {
            var userId = GetUserId();

            var order = await _dbcontext.MembershipOrders
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode
                                       && o.UserId == userId
                                       && o.Status == OrderStatus.Pending);
            if (order != null)
            {
                order.Status = OrderStatus.Cancelled;
                await _dbcontext.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        public class SePayWebhookPayload
        {
            [JsonPropertyName("id")]
            public long Id { get; set; }

            [JsonPropertyName("gateway")]
            public string? Gateway { get; set; }

            [JsonPropertyName("referenceCode")]
            public string? ReferenceCode { get; set; }

            [JsonPropertyName("transferType")]
            public string? TransferType { get; set; }

            [JsonPropertyName("transferAmount")]
            public long TransferAmount { get; set; }

            [JsonPropertyName("content")]
            public string? Content { get; set; }

            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonPropertyName("accountNumber")]
            public string? AccountNumber { get; set; }

            [JsonPropertyName("transactionDate")]
            public string? TransactionDate { get; set; }

            [JsonPropertyName("accumulated")]
            public long? Accumulated { get; set; }

            [JsonPropertyName("subAccount")]
            public string? SubAccount { get; set; }

            [JsonPropertyName("code")]
            public string? Code { get; set; }
        }

    }
    //public class SePayWebhookPayload
    //{
    //    /// <summary>Mã giao dịch nội bộ SePay</summary>
    //    /// 
    //    [JsonPropertyName("id")]
    //    public string? Id { get; set; }

    //    /// <summary>Mã tham chiếu ngân hàng</summary>
    //    [JsonPropertyName("referenceCode")]
    //    public string? ReferenceCode { get; set; }

    //    /// <summary>"in" = tiền vào, "out" = tiền ra</summary>
    //    [JsonPropertyName("transferType")]
    //    public string? TransferType { get; set; }

    //    /// <summary>Số tiền (VNĐ)</summary>
    //    [JsonPropertyName("transferAmount")]
    //    public long TransferAmount { get; set; }

    //    /// <summary>Nội dung chuyển khoản — chứa mã đơn</summary>
    //    [JsonPropertyName("content")]
    //    public string? Content { get; set; }

    //    /// <summary>Số tài khoản nhận</summary>
    //    [JsonPropertyName("accountNumber")]
    //    public string? AccountNumber { get; set; }

    //    /// <summary>Unix timestamp thời điểm giao dịch</summary>
    //    [JsonPropertyName("transactionDate")]
    //    public string? TransactionDate { get; set; }

    //    /// <summary>Số dư sau giao dịch</summary>
    //    [JsonPropertyName("accumulated")]
    //    public long? AccumulatedAmount { get; set; }

    //    /// <summary>Tên ngân hàng</summary>
    //    [JsonPropertyName("bankBrandName")]
    //    public string? BankBrandName { get; set; }
    //}
}