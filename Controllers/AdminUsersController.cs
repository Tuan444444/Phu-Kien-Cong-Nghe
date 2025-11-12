using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PhuKienCongNghe.Data; // Sửa namespace Data của bạn
using PhuKienCongNghe.Models; // Sửa namespace Models của bạn
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PhuKienCongNghe.Controllers
{
    public class AdminUsersController : Controller
    {
        private readonly PhukiencongngheDbContext _context; // Sửa tên DbContext của bạn

        public AdminUsersController(PhukiencongngheDbContext context)
        {
            _context = context;
        }

        //
        // GET: /AdminUsers/Index (Trang Danh sách)
        //
        public async Task<IActionResult> Index(string? searchString, string? role)
        {
            ViewData["Title"] = "Quản lý Người dùng";

            var query = _context.Nguoidungs.AsQueryable();

            // 1. Lọc theo Tìm kiếm (Tên, Email, hoặc TênĐăngNhập)
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(u => u.HoTen.Contains(searchString)
                                      || u.Email.Contains(searchString)
                                      || u.TenDangNhap.Contains(searchString));
            }

            // 2. Lọc theo Vai trò (Cột "VaiTro")
            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => u.VaiTro == role);
            }

            // Gửi các tùy chọn lọc ra View
            // *** SỬA NẾU VAI TRÒ CỦA BẠN KHÁC (ví dụ: "Customer" thay vì "User") ***
            ViewBag.RoleList = new List<string> { "Admin", "User" };
            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentRole = role;

            var users = await query.OrderBy(u => u.HoTen).ToListAsync();
            return View(users);
        }

        //
        // GET: /AdminUsers/Edit/5 (Trang Sửa Vai trò)
        //
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var user = await _context.Nguoidungs.FindAsync(id);
            if (user == null) return NotFound();

            // Gửi tùy chọn cho Dropdown
            ViewBag.RoleOptions = new SelectList(new List<string> { "Admin", "User" }, user.VaiTro);

            ViewData["Title"] = "Sửa Vai trò Người dùng";
            return View(user);
        }

        //
        // POST: /AdminUsers/Edit/5 (Xử lý Sửa)
        //
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaNguoiDung,VaiTro")] Nguoidung formData)
        {
            if (id != formData.MaNguoiDung) return NotFound();

            var userToUpdate = await _context.Nguoidungs.FindAsync(id);
            if (userToUpdate == null) return NotFound();

            // Chỉ cập nhật VaiTro
            userToUpdate.VaiTro = formData.VaiTro;

            try
            {
                _context.Update(userToUpdate);
                await _context.SaveChangesAsync();
                TempData["ToastMessage"] = "Cập nhật vai trò thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                TempData["ToastError"] = "Cập nhật thất bại.";
                ViewBag.RoleOptions = new SelectList(new List<string> { "Admin", "User" }, formData.VaiTro);
                return View(userToUpdate); // Trả về user đầy đủ
            }
        }

        //
        // GET: /AdminUsers/Details/5 (Xem Chi tiết & Lịch sử Đơn hàng)
        //
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Nguoidungs
                .Include(u => u.Donhangs) // Tải kèm Lịch sử Đơn hàng
                .FirstOrDefaultAsync(u => u.MaNguoiDung == id);

            if (user == null) return NotFound();

            ViewData["Title"] = "Chi tiết Người dùng";
            return View(user);
        }
    }
}
