using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PhuKienCongNghe.Data;
using PhuKienCongNghe.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
// using Microsoft.AspNetCore.Authorization;
namespace PhuKienCongNghe.Controllers
{
    public class AdminOrdersController : Controller
    {
        private readonly PhukiencongngheDbContext _context;

        public AdminOrdersController(PhukiencongngheDbContext context)
        {
            _context = context;
        }

        //
        // GET: /AdminOrders/Index
        //
        public async Task<IActionResult> Index(string status)
        {
            ViewData["Title"] = "Quản lý Đơn hàng";

            // Model "Donhang" của bạn là 'Donhang', EF Core sẽ gọi DbSet là "Donhangs"
            var query = _context.Donhangs.AsQueryable();

            var trangThaiList = new List<string> { "Đang xử lý", "Đang giao", "Hoàn thành", "Đã hủy" };
            ViewBag.TrangThaiList = trangThaiList;

            if (!string.IsNullOrEmpty(status) && trangThaiList.Contains(status))
            {
                query = query.Where(d => d.TrangThai == status);
                ViewData["Title"] = $"Đơn hàng: {status}";
            }

            ViewBag.CurrentStatus = status;

            var orders = await query
                            .OrderByDescending(d => d.NgayDat)
                            .ToListAsync();

            return View(orders);
        }

        //
        // GET: /AdminOrders/Details/5
        //
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // --- SỬA QUAN TRỌNG ---
            // Dùng đúng tên "Chitietdonhangs" (từ Model của bạn)
            var donHang = await _context.Donhangs
                .Include(d => d.Chitietdonhangs)
                    .ThenInclude(ct => ct.MaSanPhamNavigation)
                .FirstOrDefaultAsync(m => m.MaDonHang == id);

            if (donHang == null)
            {
                return NotFound();
            }

            // *** SỬA LIST NÀY CHO KHỚP VỚI CSDL CỦA BẠN ***
            ViewBag.TrangThaiOptions = new SelectList(
                new List<string> { "Đang xử lý", "Đang giao", "Hoàn thành", "Đã hủy" },

                donHang.TrangThai
            );

            ViewData["Title"] = $"Chi tiết Đơn hàng #{donHang.MaDonHang}";
            return View(donHang);
        }

        //
        // POST: /AdminOrders/UpdateStatus
        //
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int maDonHang, string trangThai)
        {
            var donHang = await _context.Donhangs.FindAsync(maDonHang);
            if (donHang == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(trangThai))
            {
                donHang.TrangThai = trangThai;
                _context.Update(donHang);
                await _context.SaveChangesAsync();
                TempData["ToastMessage"] = "Cập nhật trạng thái đơn hàng thành công!";
            }
            else
            {
                TempData["ToastError"] = "Cập nhật thất bại. Vui lòng chọn một trạng thái.";
            }

            return RedirectToAction(nameof(Details), new { id = maDonHang });
        }
    }
}