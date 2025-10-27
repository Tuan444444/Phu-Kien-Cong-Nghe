using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PhuKienCongNghe.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 2. ??ng ký DbContext
builder.Services.AddDbContext<PhukiencongngheDbContext>(options =>
    options.UseSqlServer(connectionString)); // <-- THAY ??I ? ?ÂY
// 3. ??ng ký d?ch v? cho Controllers và Views
builder.Services.AddControllersWithViews();

// 4. ??ng ký d?ch v? Session
builder.Services.AddDistributedMemoryCache(); // C?n thi?t cho session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Th?i gian session t?n t?i
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 5. ??ng ký d?ch v? HttpContextAccessor (?? l?y session trong service)
builder.Services.AddHttpContextAccessor();

// 6. C?u hình Xác th?c b?ng Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // ???ng d?n ??n trang ??ng nh?p
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Home/AccessDenied"; // Trang t? ch?i truy c?p
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); // Kích ho?t Session

app.UseAuthentication(); // Kích ho?t Xác th?c
app.UseAuthorization(); // Kích ho?t Phân quy?n

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();