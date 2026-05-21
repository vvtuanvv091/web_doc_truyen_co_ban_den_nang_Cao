using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using web_doc_truyen_Co_ban.Areas.Admin.Service;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Hubs;
using web_doc_truyen_Co_ban.Services;

var builder = WebApplication.CreateBuilder(args);

// ====================== DATABASE ======================

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ====================== IDENTITY ======================

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ====================== COOKIE ======================

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";

    options.AccessDeniedPath = "/Account/Login";
});

// ====================== MVC ======================

builder.Services.AddControllersWithViews()
    .AddJsonOptions(x =>
        x.JsonSerializerOptions.PropertyNameCaseInsensitive = true);

builder.Services.AddRazorPages();

// ====================== SESSION ======================

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);

    options.Cookie.HttpOnly = true;

    options.Cookie.IsEssential = true;
});

// ====================== SERVICES ======================

builder.Services.AddSignalR();

builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient();

builder.Services.AddScoped<VietQrService>();

builder.Services.AddScoped<ErrorAdminViewService>();

builder.Services.AddScoped<MangaDexService>();

// ====================== BUILD APP ======================

var app = builder.Build();

// ====================== ERROR ======================

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}

// ====================== MIDDLEWARE ======================

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();

app.UseAuthorization();

// ====================== ROUTES ======================

app.MapControllers();

app.MapControllerRoute(
    name: "Areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// ====================== SIGNALR ======================

app.MapHub<NotificationHub>("/notificationHub");

// ====================== SEED DATA ======================

try
{
    using var scope = app.Services.CreateScope();

    var context = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    SeedData.SeedingData(context);
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Lỗi Seed Data: {ex.Message}");

    Console.WriteLine(ex.InnerException?.Message);

    throw;
}

// ====================== RUN ======================

app.Run();