using DocumentFormat.OpenXml.Spreadsheet;

namespace web_doc_truyen_Co_ban.Hubs
{
    public class NotificationHub : Microsoft.AspNetCore.SignalR.Hub
    {
        // Client kết nối → tự join group theo userId
        public async Task JoinUserGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        public async Task LeaveUserGroup(string userId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        }
    }
}
