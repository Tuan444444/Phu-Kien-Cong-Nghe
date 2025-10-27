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

        [HttpGet]
        public IActionResult Login(string? returnUrl)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Nguoidungs.FirstOrDefaultAsync(u => u.TenDangNhap == model.TenDangNhap);

                if (user != null)
                {
                    // Lỗi bảo mật nghiêm trọng trong CSDL của bạn!
                    // Chúng ta phải kiểm tra mật khẩu đã băm
                    // Mã kiểm tra mật khẩu plaintext (KHÔNG NÊN DÙNG): if (user.MatKhau == model.MatKhau)

                    // Mã kiểm tra mật khẩu đã băm (ĐÚNG)
                    if (BCrypt.Net.BCrypt.Verify(model.MatKhau, user.MatKhau))
                    {
                        // Tạo các "Claims" (thông tin định danh)
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, user.TenDangNhap),
                            new Claim(ClaimTypes.NameIdentifier, user.MaNguoiDung.ToString()),
                            new Claim(ClaimTypes.Role, user.VaiTro) // "admin" hoặc "user"
                            // Thêm các claim khác nếu cần (Email, HoTen...)
                        };

                        var claimsIdentity = new ClaimsIdentity(
                            claims, CookieAuthenticationDefaults.AuthenticationScheme);

                        var authProperties = new AuthenticationProperties
                        {
                            // Có thể thêm các thuộc tính khác như IsPersistent (ghi nhớ đăng nhập)
                        };

                        // Đăng nhập
                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);

                        // Chuyển hướng về trang trước đó (nếu có)
                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }

                        return RedirectToAction("Index", "Home");
                    }
                }

                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra Tên đăng nhập hoặc Email đã tồn tại chưa
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

                // Băm mật khẩu trước khi lưu
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

                // Tự động đăng nhập sau khi đăng ký thành công
                var loginModel = new LoginViewModel { TenDangNhap = model.TenDangNhap, MatKhau = model.MatKhau };
                return await Login(loginModel, "/");
            }
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}