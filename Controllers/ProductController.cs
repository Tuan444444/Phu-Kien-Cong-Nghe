using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuKienCongNghe.Data;
using PhuKienCongNghe.Models; // Sử dụng namespace Models của bạn
using System.Linq;
using System.Threading.Tasks;

namespace PhuKienCongNghe.Controllers
{
    public class ProductController : Controller
    {
        // Biến này sẽ chứa "cầu nối" đến database
        // *** LƯU Ý: Thay 'PhuKienCongNgheContext' bằng tên file DbContext thật của bạn
        private readonly PhukiencongngheDbContext _context;

        // Constructor: "Tiêm" DbContext vào Controller
        public ProductController(PhukiencongngheDbContext context)
        {
            _context = context;
        }

        // GET: /Products
        // Đây là Action sẽ hiển thị danh sách sản phẩm
        public async Task<IActionResult> Index()
        {
            // Lấy 8 sản phẩm nổi bật
            // Dùng .Include() để tải "kèm" thông tin của Danh Mục (từ file Danhmuc.cs)
            var products = await _context.Sanphams
                                         .Include(s => s.MaDanhMucNavigation)
                                         .ToListAsync();

            // Gửi danh sách sản phẩm này đến file View "Index.cshtml"
            return View(products);
        }

        // GET: /Products/Details/5
        // Action này để xem chi tiết sản phẩm (sẽ cần cho các bước sau)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound(); // Trả về 404 nếu không có ID
            }

            // Tìm sản phẩm trong database theo id
            // Quan trọng: Dùng .Include() để tải "kèm" thông tin của Danh Mục
            var sanpham = await _context.Sanphams
                .Include(s => s.MaDanhMucNavigation)
                .FirstOrDefaultAsync(m => m.MaSanPham == id);

            if (sanpham == null)
            {
                return NotFound(); // Trả về 404 nếu không tìm thấy SP
            }

            // Gửi 1 đối tượng "sanpham" duy nhất này đến View
            return View(sanpham);
        }
    }
}