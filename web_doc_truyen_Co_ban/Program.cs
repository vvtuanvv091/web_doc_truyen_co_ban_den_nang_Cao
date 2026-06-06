using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using web_doc_truyen_Co_ban.Areas.Admin.Service;
using web_doc_truyen_Co_ban.Data;
using web_doc_truyen_Co_ban.Hubs;
using web_doc_truyen_Co_ban.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(connectionString));
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<IdentityUser, IdentityRole>(
    options =>
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
//laasy api console dk gg
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    });
//lay viettinpay
builder.Services.AddScoped<VietQrService>();
builder.Services.AddHttpContextAccessor();
//nafy eos bt  cx o co j
builder.Services.AddScoped<ErrorAdminViewService>();
//builder.Services.AddControllersWithViews();
builder.Services.AddControllersWithViews()
    .AddJsonOptions(x => x.JsonSerializerOptions.PropertyNameCaseInsensitive = true);
//test

builder.Services.AddSignalR();

builder.Services.AddRazorPages(); // ✅ THÊM VÀO ĐÂY
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    //// ✅ Thêm dòng này: redirect đúng chỗ tùy theo URL này tìm hieur thêm vướng
    //options.Events.OnRedirectToLogin = ctx =>
    //{
    //    if (ctx.Request.Path.StartsWithSegments("/Admin"))
    //        ctx.Response.Redirect("/Admin/Account/Login");
    //    else
    //        ctx.Response.Redirect("/Account/Login");
    //    return Task.CompletedTask;
    //};
});


//  THÊM SESSION
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpClient();
builder.Services.AddScoped<web_doc_truyen_Co_ban.Areas.Admin.Service.MangaDexService>();

var app = builder.Build();
//app.UseStatusCodePagesWithRedirects("/Home/Error?statuscode=0"); luowif tu lam trang 404 di
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles(); // THÊM DÒNG NÀY

app.UseRouting();


app.UseAuthentication(); // THÊM DÒNG NÀY (Identity cần Authentication trước Authorization)

app.UseSession();
app.MapStaticAssets();

app.MapControllers(); 

app.MapControllerRoute(
    name: "Areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();



app.MapRazorPages()
   .WithStaticAssets();
//seeding data
app.MapHub<NotificationHub>("/notificationHub");
try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    SeedData.SeedingData(context);
}
catch (Exception ex)

{
    Console.WriteLine($"❌ Lỗi Seed Data: {ex.Message}");
    Console.WriteLine(ex.InnerException?.Message);
    throw; // giữ throw để thấy full error
}
app.Run();
