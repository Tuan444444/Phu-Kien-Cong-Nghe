using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PhuKienCongNghe.Data;
using PhuKienCongNghe.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Lấy chuỗi kết nối
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 2. Đăng ký DbContext
builder.Services.AddDbContext<PhukiencongngheDbContext>(options =>
    options.UseSqlServer(connectionString));

// 3. Đăng ký dịch vụ cho Controller và View
builder.Services.AddControllersWithViews();

// 3.1. Đăng ký FeaturedProductService (do bạn thêm)
builder.Services.AddSingleton<FeaturedProductService>();

// 4. Đăng ký dịch vụ Session (Cho Giỏ Hàng)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 5. Đăng ký dịch vụ HttpContextAccessor (Để View đọc Session của giỏ hàng)
builder.Services.AddHttpContextAccessor();

// 6. ĐĂNG KÝ DỊCH VỤ XÁC THỰC (Cho Đăng Nhập bằng Cookie)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Đường dẫn đến trang đăng nhập
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Home/AccessDenied";
    });

// === XÂY DỰNG ỨNG DỤNG ===
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting(); // <-- SỐ 1: BỘ ĐỊNH TUYẾN

// === KHỐI QUAN TRỌNG: PHẢI ĐÚNG THỨ TỰ ===
app.UseSession(); // <-- SỐ 2: Kích hoạt Session (cho giỏ hàng)
app.UseAuthentication(); // <-- SỐ 3: Kích hoạt Xác thực (để đọc Cookie)
app.UseAuthorization();  // <-- SỐ 4: Kích hoạt Phân quyền

// =======================================
app.MapControllerRoute( // <-- SỐ 5: PHẢI NẰM CUỐI CÙNG
    name: "default",
    pattern: "{controller=Product}/{action=Index}/{id?}");

app.Run();
