using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuKienCongNghe.Data; 
using PhuKienCongNghe.Models; 
using PhuKienCongNghe.ViewModels;
using System.Linq;
using System.Threading.Tasks;
namespace PhuKienCongNghe.Controllers
{
    public class AdminController : Controller
    {
        private readonly PhukiencongngheDbContext _context; // Sửa tên DbContext của bạn

        public AdminController(PhukiencongngheDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Bảng điều khiển";
            var viewModel = new DashboardViewModel();

            var homNay = DateTime.Today;
            var dauThangNay = new DateTime(homNay.Year, homNay.Month, 1);
            var cuoiThangNay = dauThangNay.AddMonths(1).AddDays(-1);

            // --- 1. HỘP 1: Doanh thu THÁNG NÀY --- (SỬA)
            // (Chỉ tính doanh thu cho đơn "Hoàn thành" trong tháng)
            viewModel.DoanhThuThangNay = await _context.Donhangs
                .Where(d => d.TrangThai == "Hoàn thành" &&
                            d.NgayDat.HasValue &&
                            d.NgayDat.Value.Date >= dauThangNay &&
                            d.NgayDat.Value.Date <= cuoiThangNay)
                .SumAsync(d => d.TongTien);

            // --- 2. HỘP 2: Đơn hàng mới --- (Giữ nguyên)
            viewModel.DonHangMoi = await _context.Donhangs
                .CountAsync(d => d.TrangThai == "Chờ xác nhận");

            // --- 3. HỘP 3: Tổng số Khách hàng --- (Giữ nguyên)
            viewModel.TongKhachHang = await _context.Nguoidungs
                .CountAsync(u => u.VaiTro == "User");

            // --- 4. HỘP 4: Sắp hết hàng --- (MỚI)
            // *** QUAN TRỌNG: Giả định bạn có cột "SoLuongTon" trong bảng "Sanpham"
            //     và ngưỡng sắp hết hàng là 5.
            //     Sửa "SoLuongTon" nếu cột của bạn tên khác (ví dụ: TonKho)
            viewModel.SanPhamSapHetHang = await _context.Sanphams
                .CountAsync(p => p.SoLuongTon <= 10); // <-- Sửa "SoLuongTon" và số 5 nếu cần

            return View(viewModel);
        }
        [HttpGet]
        public async Task<IActionResult> GetRevenueChartData()
        {
            var homNay = DateTime.Today;
            var labels = new List<string>(); // Chứa nhãn (T2, T3, T4...)
            var data = new List<double>(); // Chứa dữ liệu (Doanh thu)

            // Lặp 7 ngày (từ 6 ngày trước đến hôm nay)
            for (int i = 6; i >= 0; i--)
            {
                var ngay = homNay.AddDays(-i);

                // Tính tổng doanh thu của ngày đó (chỉ đơn "Hoàn thành")
                var doanhThuNgay = await _context.Donhangs
                    .Where(d => d.TrangThai == "Hoàn thành" &&
                                d.NgayDat.HasValue &&
                                d.NgayDat.Value.Date == ngay.Date)
                    .SumAsync(d => d.TongTien);

                // Thêm nhãn (ví dụ: "09/11")
                labels.Add(ngay.ToString("dd/MM"));

                // Thêm dữ liệu (ví dụ: 500000)
                data.Add(doanhThuNgay);
            }

            // Trả về dữ liệu dạng JSON
            return Json(new { Labels = labels, Data = data });
        }


        [HttpGet]
        public async Task<IActionResult> GetTopProductsChartData()
        {
            // 1. Truy vấn CSDL để lấy Top 5 SP (theo số lượng bán)
            var topProductsData = await _context.Chitietdonhangs // Từ bảng Chi Tiết
                .GroupBy(c => c.MaSanPham) // Nhóm theo Mã SP
                .Select(g => new
                {
                    MaSanPham = g.Key,
                    TongSoLuong = g.Sum(c => c.SoLuong) // Tính tổng số lượng
                })
                .OrderByDescending(x => x.TongSoLuong) // Sắp xếp
                .Take(5) // Lấy 5
                .ToListAsync();

            var labels = new List<string>(); // Chứa Tên SP
            var data = new List<int>(); // Chứa Số lượng

            // 2. Lấy Tên sản phẩm cho 5 SP top đầu
            // (Sắp xếp lại theo thứ tự tăng dần để hiển thị
            //  đúng trên biểu đồ cột ngang (item cao nhất ở trên cùng))
            foreach (var item in topProductsData.OrderBy(x => x.TongSoLuong))
            {
                var sanpham = await _context.Sanphams.FindAsync(item.MaSanPham);
                if (sanpham != null)
                {
                    labels.Add(sanpham.TenSanPham); // Thêm Tên
                    data.Add(item.TongSoLuong); // Thêm Số lượng
                }
            }

            // 3. Trả về JSON
            return Json(new { Labels = labels, Data = data });
        }
        [HttpGet]
        public async Task<IActionResult> GetCategoryRevenueData()
        {
            // 1. Lấy tất cả chi tiết đơn hàng TỪ các đơn "Hoàn thành"
            var details = await _context.Chitietdonhangs
                .Include(c => c.MaDonHangNavigation) // Nối bảng Đơn hàng
                .Include(c => c.MaSanPhamNavigation) // Nối bảng Sản phẩm
                    .ThenInclude(p => p.MaDanhMucNavigation) // Từ SP, nối bảng Danh mục
                .Where(c => c.MaDonHangNavigation.TrangThai == "Hoàn thành")
                .ToListAsync();

            // 2. Nhóm các chi tiết đó theo Tên Danh mục và Tính tổng
            var data = details
                .GroupBy(c => c.MaSanPhamNavigation?.MaDanhMucNavigation?.TenDanhMuc ?? "Danh mục khác")
                .Select(g => new
                {
                    TenDanhMuc = g.Key,
                    TongDoanhThu = g.Sum(c => c.SoLuong * c.DonGia)
                })
                .ToList();

            // 3. Chuẩn bị dữ liệu cho Chart.js
            var labels = data.Select(d => d.TenDanhMuc).ToList();
            var chartData = data.Select(d => d.TongDoanhThu).ToList();

            // 4. Trả về JSON
         
            return Json(new { Labels = labels, Data = chartData });
        }
        public async Task<IActionResult> GetOrderStatusData()
        {
            // 1. Nhóm tất cả đơn hàng theo Trạng thái và Đếm
            var data = await _context.Donhangs
                .GroupBy(d => d.TrangThai)
                .Select(g => new {
                    TrangThai = g.Key,
                    SoLuong = g.Count()
                })
                .OrderByDescending(x => x.SoLuong) // Sắp xếp cho đẹp
                .ToListAsync();

            // 2. Chuẩn bị dữ liệu cho Chart.js
            var labels = data.Select(d => d.TrangThai).ToList();
            var chartData = data.Select(d => d.SoLuong).ToList();

            // 3. Trả về JSON
            return Json(new { Labels = labels, Data = chartData });
        }
    }
}
