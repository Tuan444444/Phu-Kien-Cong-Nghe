using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuKienCongNghe.Data;
using PhuKienCongNghe.Models;
using PhuKienCongNghe.ViewModels;
using System.Security.Claims;

namespace PhuKienCongNghe.Controllers
{
    public class AccountController : Controller
    {
        private readonly PhukiencongngheDbContext _context;

        public AccountController(PhukiencongngheDbContext context)
        {
            _context = context;
        }

        //===========================================================
        // CHỨC NĂNG LOGIN
        //===========================================================

        [HttpGet]
        public IActionResult Login(string? returnUrl)
        {
            // Lưu lại returnUrl để chuyển hướng người dùng về trang họ muốn sau khi login
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = await _context.Nguoidungs
                                 .FirstOrDefaultAsync(u => u.TenDangNhap == model.TenDangNhap);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Tên đăng nhập không tồn tại.");
                    return View(model);
                }

                // Kiểm tra mật khẩu đã băm
                // (Giả định bạn đã cài: dotnet add package BCrypt.Net-Next)
                if (BCrypt.Net.BCrypt.Verify(model.MatKhau, user.MatKhau))
                {
                    // Tạo các "Claims" (thông tin định danh của user)
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.MaNguoiDung.ToString()),
                        new Claim(ClaimTypes.Name, user.TenDangNhap),
                        new Claim(ClaimTypes.Role, user.VaiTro)
                        // Bạn có thể thêm các Claim khác như Email, HoTen...
                    };

                    // Tạo ClaimsIdentity
                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        // Cho phép "Ghi nhớ tôi" (chưa làm)
                        // IsPersistent = model.RememberMe
                    };

                    // Đăng nhập người dùng (tạo cookie xác thực)
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    // Chuyển hướng về trang họ muốn
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    // Nếu không, về trang chủ
                    return RedirectToAction("Index", "Home");
                }

                // Sai mật khẩu
                ModelState.AddModelError(string.Empty, "Sai mật khẩu.");
                return View(model);
            }

            // Nếu model không hợp lệ (ví dụ: bỏ trống), hiển thị lại form
            return View(model);
        }

        //===========================================================
        // CHỨC NĂNG LOGOUT
        //===========================================================

        // Dùng HttpPost để tránh lỗi CSRF
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Xóa cookie xác thực
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            // Về trang chủ
            return RedirectToAction("Index", "Home");
        }

        //===========================================================
        // CHỨC NĂNG REGISTER (Thêm vào để _LoginPartial hoạt động)
        //===========================================================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra Tên đăng nhập hoặc Email đã tồn tại
                if (await _context.Nguoidungs.AnyAsync(u => u.TenDangNhap == model.TenDangNhap))
                {
                    ModelState.AddModelError("TenDangNhap", "Tên đăng nhập đã tồn tại.");
                    return View(model);
                }
                if (await _context.Nguoidungs.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email đã tồn tại.");
                    return View(model);
                }

                // Băm mật khẩu
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.MatKhau);

                var user = new Nguoidung
                {
                    HoTen = model.HoTen,
                    TenDangNhap = model.TenDangNhap,
                    Email = model.Email,
                    MatKhau = hashedPassword, // Lưu mật khẩu đã băm
                    SoDienThoai = model.SoDienThoai,
                    DiaChi = model.DiaChi,
                    VaiTro = "user"
                };

                _context.Nguoidungs.Add(user);
                await _context.SaveChangesAsync();

                // Tự động đăng nhập sau khi đăng ký
                var loginModel = new LoginViewModel { TenDangNhap = model.TenDangNhap, MatKhau = model.MatKhau };
                // Chuyển đến Action Login (Post)
                return await Login(loginModel, "/");
            }
            return View(model);
        }
    }
}