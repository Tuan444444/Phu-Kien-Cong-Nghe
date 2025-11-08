using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuKienCongNghe.Data; // Thay bằng namespace Data của bạn
using System.Linq;
using System.Threading.Tasks;
namespace PhuKienCongNghe.Controllers
{
    public class AdminController : Controller
    {
        private readonly PhukiencongngheDbContext _context;

        public AdminController(PhukiencongngheDbContext context)
        {
            _context = context;
        }

        //
        // HÀM HIỂN THỊ TRANG DASHBOARD
        //
        public async Task<IActionResult> Index()
        {
            // --- 1. Lấy 4 Thẻ Thống Kê Nhanh ---

            // A. Doanh thu 30 ngày qua
            var totalRevenue = await _context.Donhangs
                .Where(dh => dh.NgayDat >= DateTime.Now.AddDays(-30) && dh.TrangThai == "Đã hoàn thành")
                .SumAsync(dh => dh.TongTien);

            // B. Đơn hàng Mới (Chờ duyệt)
            var newOrders = await _context.Donhangs
                .CountAsync(dh => dh.TrangThai == "Chờ xác nhận");

            // C. Tổng số Khách hàng
            var totalUsers = await _context.Nguoidungs.CountAsync(); // (Thay Nguoidungs bằng bảng user của bạn)

            // D. Sản phẩm sắp hết
            var lowStock = await _context.Sanphams
                .CountAsync(sp => sp.SoLuongTon <= 10); // (Giả sử mốc là 10)

            // --- 2. Lấy 5 Đơn hàng Mới nhất ---
            var latestOrders = await _context.Donhangs
                .Where(dh => dh.TrangThai == "Chờ xác nhận")
                .OrderByDescending(dh => dh.NgayDat)
                .Take(5)
                .ToListAsync();

            // --- 3. Gửi dữ liệu qua Bằng ViewData/ViewBag ---
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.NewOrders = newOrders;
            ViewBag.TotalUsers = totalUsers;
            ViewBag.LowStock = lowStock;

            // Gửi danh sách 5 đơn hàng mới
            return View(latestOrders); // Model của View này sẽ là List<Donhang>
        }
    }
}
