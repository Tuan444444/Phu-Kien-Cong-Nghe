using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuKienCongNghe.Data;

namespace PhuKienCongNghe.Controllers
{
    public class ProductController : Controller
    {
        private readonly PhukiencongngheDbContext _context;

        public ProductController(PhukiencongngheDbContext context)
        {
            _context = context;
        }

        // Trang hiển thị sản phẩm theo danh mục
        // VD: /Product/List/1 (1 là MaDanhMuc)
        public async Task<IActionResult> List(int id)
        {
            var category = await _context.Danhmucs.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var products = await _context.Sanphams
                                         .Where(p => p.MaDanhMuc == id)
                                         .ToListAsync();

            ViewBag.CategoryName = category.TenDanhMuc;
            return View(products);
        }

        // Trang chi tiết sản phẩm
        // VD: /Product/Details/212 (212 là MaSanPham)
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Sanphams
                                        .Include(p => p.MaDanhMucNavigation) // Lấy luôn thông tin Danh Mục
                                        .FirstOrDefaultAsync(p => p.MaSanPham == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
    }
}