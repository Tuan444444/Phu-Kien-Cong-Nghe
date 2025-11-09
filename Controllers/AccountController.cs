using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuKienCongNghe.Data;
using PhuKienCongNghe.Models;
using PhuKienCongNghe.ViewModels;
using System.Security.Claims; // Bắt buộc phải có

namespace PhuKienCongNghe.Controllers
{
    public class AccountController : Controller
    {
        private readonly PhukiencongngheDbContext _context;

        public AccountController(PhukiencongngheDbContext context)
        {
            _context = context;
        }

        //===========================
        // 1. LOGIN (GET)
        //===========================
        [HttpGet]
        public IActionResult Login(string? returnUrl)
        {
            // Lưu lại returnUrl để chuyển hướng người dùng về trang họ muốn sau khi login
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        //===========================
        // 2. LOGIN (POST)
        //===========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                // 1. Tìm user trong CSDL
                var user = await _context.Nguoidungs
                    .FirstOrDefaultAsync(u => u.TenDangNhap == model.TenDangNhap);

                // 2. Kiểm tra user có tồn tại và mật khẩu có khớp (dùng BCrypt)
                if (user != null && BCrypt.Net.BCrypt.Verify(model.MatKhau, user.MatKhau))
                {
                    // 3. TẠO "VÉ VÀO CỬA" (Claims)
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.TenDangNhap),
                        new Claim(ClaimTypes.NameIdentifier, user.MaNguoiDung.ToString()),
                        new Claim(ClaimTypes.Role, user.VaiTro)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties();

                    // 4. "PHÁT VÉ" CHO TRÌNH DUYỆT
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    // 5. Tạo thông báo
                    TempData["SuccessMessage"] = "Đăng nhập thành công!";

                    // 6. Logic phân quyền
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    if (user.VaiTro == "admin")
                    {
                        return RedirectToAction("Index", "Admin");
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }

                // Nếu sai mật khẩu hoặc không tìm thấy user
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không chính xác.");
            }

            // Nếu model không hợp lệ
            return View(model);
        }

        //===========================
        // 3. REGISTER (GET)
        //===========================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        //===========================
        // 4. REGISTER (POST)
        //===========================
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
                    MatKhau = hashedPassword,
                    SoDienThoai = model.SoDienThoai,
                    DiaChi = model.DiaChi,
                    VaiTro = "user"
                };

                _context.Nguoidungs.Add(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            return View(model);
        }

        //===========================
        // 5. LOGOUT
        //===========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
