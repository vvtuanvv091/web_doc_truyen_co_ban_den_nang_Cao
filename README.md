# 📚 Web Đọc Truyện
Ứng dụng web đọc truyện được xây dựng bằng ASP.NET Core MVC.
Link Website: https://web-doc-truyen-co-ban-den-nang-cao.onrender.com/
## 🛠️ Công nghệ sử dụng
- **Backend:** ASP.NET Core MVC (.NET)
- **Frontend:** HTML, CSS, Bootstrap 5, JavaScript
- **Database:** Entity Framework Core (Code First)
- **Thanh-Toan:** SePay (NotificationHub)
- **Real-time:** SignalR (NotificationHub)
- **Xác thực:** ASP.NET Core Identity
- **Icon:** Bootstrap Icons, Font Awesome

## ✨ Tính năng
- Xem danh sách và đọc truyện
- Đăng ký, đăng nhập tài khoản
- Đánh dấu truyện yêu thích
- Thanh toan QR
- thễm,xóa,sửa
- Thông báo real-time bằng SignalR
- Trang (Admin,user) quản lý nội dung
## 📁 Cấu trúc project
```
web_doc_truyen_Co_ban/
├── Areas/
│   ├── Admin/          # Khu vực quản trị
│   ├── Author/         # Khu vực tác giả
│   ├── Identity/       # Xác thực người dùng
│   └── Support_IT/     # Hỗ trợ kỹ thuật
├── Controllers/        # Các Controller MVC
├── Data/
│   ├── Components/     # View Components
│   ├── Migrations/     # Migration database
│   └── ApplicationDbContext.cs
├── DTO/                # Data Transfer Objects
├── Hubs/               # SignalR Hubs
├── Models/             # Các Model dữ liệu
├── Services/           # Xử lý nghiệp vụ
├── Views/              # Razor Views
├── wwwroot/            # File tĩnh (CSS, JS, ảnh)
└── Program.cs
```
## 🚀 Hướng dẫn chạy project
 
### Yêu cầu
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/en-us/sql-server)
- [Visual Studio 2022](https://visualstudio.microsoft.com/)
### Các bước cài đặt
 
1. **Clone repository về máy**
   git clone https://github.com/ten-cua-ban/web_doc_truyen_Co_ban.git
   cd web_doc_truyen_Co_ban

2. **Cấu hình chuỗi kết nối** trong file `appsettings.json`
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=TEN_SERVER;Database=web_doc_truyen;Trusted_Connection=True;"
   }

3. **Tạo database bằng Migration**
  - Database-Migration ...
  - Update-Database
 
5. **Chạy ứng dụng**
  - Nhấn **F5** trong Visual Studio.
6. Mở trình duyệt tại `https://localhost:5001`
