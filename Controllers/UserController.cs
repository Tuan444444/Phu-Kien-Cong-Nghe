using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuKienCongNghe.Data;
using PhuKienCongNghe.Models;
using PhuKienCongNghe.ViewModels; // Chúng ta sẽ tạo file ProfileViewModel
using System.Linq;
using System.Security.Claims; // Cần để lấy ID người dùng
using System.Threading.Tasks;

namespace PhuKienCongNghe.Controllers
{
    [Authorize] // BẮT BUỘC: Yêu cầu đăng nhập cho TẤT CẢ action trong Controller này
    public class UserController : Controller
    {
        private readonly PhukiencongngheDbContext _context;

        public UserController(PhukiencongngheDbContext context)
        {
            _context = context;
        }

        // === HÀM HỖ TRỢ: Lấy ID của người dùng đang đăng nhập ===
        private int GetCurrentUserId()
        {
            // Lấy ID từ "Claim" (đã được lưu trong Cookie lúc đăng nhập)
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Chuyển đổi từ string sang int
            if (int.TryParse(userIdString, out int userId))
            {
                return userId;
            }
            // Trả về -1 hoặc ném lỗi nếu không tìm thấy ID
            return -1;
        }

        //===========================================================
        // 1. CHỨC NĂNG: THÔNG TIN CÁ NHÂN (PROFILE)
        //===========================================================

        // GET: /User/Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = GetCurrentUserId();
            if (userId == -1)
            {
                // Nếu không lấy được ID (dù đã authorize), chuyển về trang chủ
                return RedirectToAction("Index", "Home");
            }

            var user = await _context.Nguoidungs.FindAsync(userId);
            if (user == null)
            {
                return NotFound("Không tìm thấy người dùng.");
            }

            // Dùng ViewModel để chỉ gửi các thông tin cần thiết, KHÔNG gửi mật khẩu
            var model = new ProfileViewModel
            {
                HoTen = user.HoTen,
                TenDangNhap = user.TenDangNhap,
                Email = user.Email,
                SoDienThoai = user.SoDienThoai,
                DiaChi = user.DiaChi
            };

            return View(model);
        }

        // POST: /User/Profile (Khi người dùng bấm "Lưu thay đổi")
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Nếu dữ liệu không hợp lệ (ví dụ: Email sai định dạng)
                // Trả về View với model hiện tại để hiển thị lỗi
                return View(model);
            }

            var userId = GetCurrentUserId();
            var user = await _context.Nguoidungs.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            // Cập nhật thông tin từ ViewModel vào Model (Nguoidung)
            user.HoTen = model.HoTen;
            user.SoDienThoai = model.SoDienThoai;
            user.DiaChi = model.DiaChi;
            // Chúng ta không cho phép đổi TenDangNhap hoặc Email ở đây cho đơn giản

            try
            {
                _context.Update(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra, vui lòng thử lại.");
            }

            return RedirectToAction("Profile");
        }


        //===========================================================
        // 2. CHỨC NĂNG: LỊCH SỬ MUA HÀNG
        //===========================================================

        // GET: /User/OrderHistory
        [HttpGet]
        public async Task<IActionResult> OrderHistory()
        {
            var userId = GetCurrentUserId();
            if (userId == -1)
            {
                return RedirectToAction("Index", "Home");
            }

            // Lấy danh sách đơn hàng của người dùng này
            var orders = await _context.Donhangs
                .Where(d => d.MaNguoiDung == userId)
                .OrderByDescending(d => d.NgayDat) // Sắp xếp mới nhất lên trên
                .Include(d => d.Chitietdonhangs) // Tải kèm Chi tiết đơn hàng
                    .ThenInclude(ct => ct.MaSanPhamNavigation) // Tải kèm thông tin Sản phẩm
                .ToListAsync();

            return View(orders);
        }

        // ... (code của hàm Profile ở trên) ...

        //===========================================================
        // 3. CHỨC NĂNG: ĐỔI MẬT KHẨU (GET)
        //===========================================================

        // GET: /User/ChangePassword
        [HttpGet]
        public IActionResult ChangePassword()
        {
            // Chỉ hiển thị form
            return View();
        }

        //===========================================================
        // 4. CHỨC NĂNG: ĐỔI MẬT KHẨU (POST)
        //===========================================================

        // POST: /User/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Nếu form không hợp lệ (bỏ trống, mật khẩu không khớp)
                // trả về view với các lỗi
                return View(model);
            }

            var userId = GetCurrentUserId(); // Dùng lại hàm GetCurrentUserId() đã có
            var user = await _context.Nguoidungs.FindAsync(userId);

            if (user == null)
            {
                return NotFound("Không tìm thấy người dùng.");
            }

            // 1. Kiểm tra mật khẩu HIỆN TẠI có đúng không
            var isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.MatKhau);

            if (!isCurrentPasswordValid)
            {
                // Nếu mật khẩu hiện tại sai -> Thêm lỗi và trả về
                ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không chính xác.");
                return View(model);
            }

            // 2. Mật khẩu hiện tại đã đúng -> Băm và cập nhật mật khẩu MỚI
            var newHashedPassword = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            user.MatKhau = newHashedPassword;

            _context.Update(user);
            await _context.SaveChangesAsync();

            // 3. Gửi thông báo thành công và chuyển hướng
            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Profile"); // Chuyển về trang thông tin cá nhân
        }
    }
}