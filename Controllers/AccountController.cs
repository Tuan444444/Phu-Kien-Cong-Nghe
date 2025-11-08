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

                // 2. Kiểm tra user có tồn tại và Mật khẩu có khớp (dùng BCrypt)
                if (user != null && BCrypt.Net.BCrypt.Verify(model.MatKhau, user.MatKhau))
                {
                    // 3. TẠO "VÉ VÀO CỬA" (Claims)
                    // Đây là các thông tin sẽ được mã hóa và lưu vào Cookie
                    var claims = new List<Claim>
                    {
                        // Lấy TenDangNhap lưu vào ô "Tên"
                        new Claim(ClaimTypes.Name, user.TenDangNhap),
                        
                        // Lấy MaNguoiDung lưu vào ô "ID"
                        new Claim(ClaimTypes.NameIdentifier, user.MaNguoiDung.ToString()),
                        
                        // Lấy VaiTro lưu vào ô "Vai trò"
                        new Claim(ClaimTypes.Role, user.VaiTro)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        // Có thể thêm các thuộc tính khác như "IsPersistent" (Ghi nhớ đăng nhập)
                    };

                    // 4. "PHÁT VÉ" CHO TRÌNH DUYỆT
                    // Dòng này sẽ tạo ra Cookie xác thực
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    // 5. Tạo thông báo (bạn yêu cầu)
                    TempData["SuccessMessage"] = "Đăng nhập thành công!";

                    // ... (Ngay sau dòng TempData["SuccessMessage"] = "Đăng nhập thành công!";)

                    // === LOGIC PHÂN QUYỀN MỚI ===

                    // 1. Kiểm tra xem người dùng có đang cố truy cập một trang cụ thể không
                    // (Ví dụ: họ đang ở /Checkout thì bị bắt đăng nhập)
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    // 2. Nếu không, chuyển hướng dựa trên vai trò (Role)
                    // (Chúng ta dùng biến 'user' mà chúng ta đã lấy từ CSDL ở đầu hàm)
                    if (user.VaiTro == "admin")
                    {
                        // Nếu là "admin", chuyển đến trang Index của AdminController
                        return RedirectToAction("Index", "Admin");
                    }
                    else
                    {
                        // Nếu là "user" (hoặc vai trò khác), chuyển về trang chủ
                        return RedirectToAction("Index", "Home");
                    }
                    // === KẾT THÚC LOGIC MỚI ===
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

                // Băm mật khẩu (Rất quan trọng)
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.MatKhau);

                var user = new Nguoidung
                {
                    HoTen = model.HoTen,
                    TenDangNhap = model.TenDangNhap,
                    Email = model.Email,
                    MatKhau = hashedPassword, // Lưu mật khẩu đã băm
                    SoDienThoai = model.SoDienThoai,
                    DiaChi = model.DiaChi,
                    VaiTro = "user" // Mặc định là user
                };

                _context.Nguoidungs.Add(user);
                await _context.SaveChangesAsync();

                // Đăng ký xong, gửi thông báo và bắt người dùng đăng nhập
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
            // "HỦY VÉ" (Xóa Cookie)
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}