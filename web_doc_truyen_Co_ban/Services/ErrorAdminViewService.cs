
using web_doc_truyen_Co_ban.Data;

namespace web_doc_truyen_Co_ban.Services
{

    public class ErrorAdminViewService
    {
        private readonly ApplicationDbContext _db;
        private readonly IHttpContextAccessor _http;
        public ErrorAdminViewService(ApplicationDbContext db, IHttpContextAccessor http)
        {
            _db = db;
            _http = http;
        }
        //// GHI LOG THÀNH CÔNG
        public async Task LogAsync(
            Guid? userId,
            string action,
            string? entityType = null,
            Guid? entityId = null)
        {
            await WriteAsync(userId, action, entityType, entityId, true, null);
        }

        //  GHI LOG THẤT BẠI
        public async Task LogErrorAsync(
            Guid? userId,
            string action,
            string? entityType = null,
            Guid? entityId = null,
            string? errorMessage = null)
        {
            await WriteAsync(userId, action, entityType, entityId, false, errorMessage);
        }

        //  GHI VÀO DB
        private async Task WriteAsync(
            Guid? userId,
            string action,
            string? entityType,
            Guid? entityId,
            bool isSuccess,
            string? errorMessage)
        {
            var ctx = _http.HttpContext;

            var log = new Models.ErrorLogAdminViewModel
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                // Lấy IP thật kể cả khi dùng proxy/nginx
                IpAddress = ctx?.Connection.RemoteIpAddress?.ToString()
                               ?? ctx?.Request.Headers["X-Forwarded-For"].FirstOrDefault(),
                UserAgent = ctx?.Request.Headers["User-Agent"].FirstOrDefault(),
                CreatedAt = DateTime.UtcNow
            };

            _db.ErrorsAdmin.Add(log);
            await _db.SaveChangesAsync();
        }

    }

}
