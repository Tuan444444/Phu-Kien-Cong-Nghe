using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuKienCongNghe.Data;
using PhuKienCongNghe.Models;
using PhuKienCongNghe.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using System; // (Thêm using này)
using System.Collections.Generic; // (Thêm using này)

namespace PhuKienCongNghe.Controllers
{
    public class AdminController : Controller
    {
        private readonly PhukiencongngheDbContext _context; // Sửa tên DbContext của bạn

        public AdminController(PhukiencongngheDbContext context)
        {
            _context = context;
        }

        //
        // --- HÀM 1: CHO 4 HỘP ---
        //
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Bảng điều khiển";
            var viewModel = new DashboardViewModel();

            var homNay = DateTime.Today;
            var dauThangNay = new DateTime(homNay.Year, homNay.Month, 1);
            var cuoiThangNay = dauThangNay.AddMonths(1).AddDays(-1);

            // Hộp 1: Doanh thu tháng này
            viewModel.DoanhThuThangNay = await _context.Donhangs
                .Where(d => d.TrangThai == "Hoàn thành" &&
                            d.NgayDat.HasValue &&
                            d.NgayDat.Value.Date >= dauThangNay &&
                            d.NgayDat.Value.Date <= cuoiThangNay)
                .SumAsync(d => d.TongTien);

            // Hộp 2: Đơn hàng mới
            viewModel.DonHangMoi = await _context.Donhangs
                .CountAsync(d => d.TrangThai == "Đang xử lý");

            // Hộp 3: Tổng số Khách hàng
            viewModel.TongKhachHang = await _context.Nguoidungs
                .CountAsync(u => u.VaiTro == "User");

            // Hộp 4: Sắp hết hàng
            viewModel.SanPhamSapHetHang = await _context.Sanphams
                .CountAsync(p => p.SoLuongTon <= 10); // <-- Sửa "SoLuongTon" nếu cột của bạn tên khác

            return View(viewModel);
        }

        //
        // --- HÀM 2: BIỂU ĐỒ DOANH THU 7 NGÀY (Line Chart) ---
        //
        [HttpGet]
        public async Task<IActionResult> GetRevenueChartData()
        {
            var homNay = DateTime.Today;
            var labels = new List<string>();
            var data = new List<double>();

            for (int i = 6; i >= 0; i--)
            {
                var ngay = homNay.AddDays(-i);
                var doanhThuNgay = await _context.Donhangs
                    .Where(d => d.TrangThai == "Hoàn thành" &&
                                d.NgayDat.HasValue &&
                                d.NgayDat.Value.Date == ngay.Date)
                    .SumAsync(d => d.TongTien);

                labels.Add(ngay.ToString("dd/MM"));
                data.Add(doanhThuNgay);
            }
            return Json(new { Labels = labels, Data = data });
        }

        //
        // --- HÀM 3: TOP 5 SẢN PHẨM (Horizontal Bar Chart) ---
        //
        [HttpGet]
        public async Task<IActionResult> GetTopProductsChartData()
        {
            var topProductsData = await _context.Chitietdonhangs
                .GroupBy(c => c.MaSanPham)
                .Select(g => new
                {
                    MaSanPham = g.Key,
                    TongSoLuong = g.Sum(c => c.SoLuong)
                })
                .OrderByDescending(x => x.TongSoLuong)
                .Take(5)
                .ToListAsync();

            var labels = new List<string>();
            var data = new List<int>();

            foreach (var item in topProductsData.OrderBy(x => x.TongSoLuong))
            {
                var sanpham = await _context.Sanphams.FindAsync(item.MaSanPham);
                if (sanpham != null)
                {
                    labels.Add(sanpham.TenSanPham);
                    data.Add(item.TongSoLuong);
                }
            }
            return Json(new { Labels = labels, Data = data });
        }

        //
        // --- HÀM 4: CƠ CẤU DOANH THU (Doughnut Chart) ---
        //
        [HttpGet]
        public async Task<IActionResult> GetCategoryRevenueData()
        {
            var details = await _context.Chitietdonhangs
                .Include(c => c.MaDonHangNavigation)
                .Include(c => c.MaSanPhamNavigation)
                    .ThenInclude(p => p.MaDanhMucNavigation)
                .Where(c => c.MaDonHangNavigation.TrangThai == "Hoàn thành")
                .ToListAsync();

            var data = details
                .GroupBy(c => c.MaSanPhamNavigation?.MaDanhMucNavigation?.TenDanhMuc ?? "Danh mục khác")
                .Select(g => new
                {
                    TenDanhMuc = g.Key,
                    TongDoanhThu = g.Sum(c => c.SoLuong * c.DonGia)
                })
                .ToList();

            var labels = data.Select(d => d.TenDanhMuc).ToList();
            var chartData = data.Select(d => d.TongDoanhThu).ToList();

            return Json(new { Labels = labels, Data = chartData });
        }

        //
        // --- HÀM 5: TÌNH TRẠNG ĐƠN HÀNG (Pie Chart) ---
        //
        [HttpGet]
        public async Task<IActionResult> GetOrderStatusData()
        {
            var data = await _context.Donhangs
                .GroupBy(d => d.TrangThai)
                .Select(g => new {
                    TrangThai = g.Key,
                    SoLuong = g.Count()
                })
                .OrderByDescending(x => x.SoLuong)
                .ToListAsync();

            var labels = data.Select(d => d.TrangThai).ToList();
            var chartData = data.Select(d => d.SoLuong).ToList();

            return Json(new { Labels = labels, Data = chartData });
        }
    }
}