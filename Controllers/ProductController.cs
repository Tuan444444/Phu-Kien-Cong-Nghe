using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuKienCongNghe.Data;
using PhuKienCongNghe.Models; // Sử dụng namespace Models của bạn
using System.Globalization; // Xóa dấu
using System.Linq;
using System.Text; // Dùng cho
using System.Threading.Tasks;
using X.PagedList.Extensions;
using PhuKienCongNghe.Services;
using PhuKienCongNghe.ViewModels;
using X.PagedList;
using System.Collections.Generic;

namespace PhuKienCongNghe.Controllers
{
    public class ProductController : Controller
    {
        // Biến này sẽ chứa "cầu nối" đến database
        // *** LƯU Ý: Thay 'PhuKienCongNgheContext' bằng tên file DbContext thật của bạn
        private readonly PhukiencongngheDbContext _context;
        private readonly FeaturedProductService _featuredService;

        // Constructor: "Tiêm" DbContext vào Controller
        public ProductController(PhukiencongngheDbContext context, FeaturedProductService featuredService)
        {
            _context = context;
            _featuredService = featuredService;
        }

        // GET: /Products
        // Đây là Action sẽ hiển thị danh sách sản phẩm
        public IActionResult Index(int? page) // Bỏ "async Task<...>"
        {
            ViewData["Title"] = "Tất cả sản phẩm";
            var viewModel = new ShopViewModel();

            // --- PHẦN 1: LẤY SẢN PHẨM NỔI BẬT (TỪ FILE JSON) ---
            var featuredEntries = _featuredService.GetAll()
                .Take(8)
                .ToList();

            var featuredIds = featuredEntries.Select(f => f.MaSanPham).ToList();

            // SỬA: "Join" bằng tay (dùng cách không async)
            var sanphamsDictionary = _context.Sanphams
                .Where(p => featuredIds.Contains(p.MaSanPham))
                .ToDictionary(p => p.MaSanPham); // Bỏ ".ToDictionaryAsync"

            foreach (var entry in featuredEntries)
            {
                if (sanphamsDictionary.TryGetValue(entry.MaSanPham, out var sanpham))
                {
                    entry.Sanpham = sanpham;
                }
            }
            viewModel.FeaturedProducts = featuredEntries.Where(e => e.Sanpham != null).ToList();

            // --- PHẦN 2: LẤY TẤT CẢ SẢN PHẨM (PHÂN TRANG TỪ SQL) ---
            int pageSize = 12;
            int pageNumber = (page ?? 1);
            var allProducts = _context.Sanphams
                                    .Include(s => s.MaDanhMucNavigation)
                                    .OrderByDescending(s => s.MaSanPham);

            // SỬA: Dùng "ToPagedList" (giống hàm gốc của bạn)
            viewModel.AllProducts = allProducts.ToPagedList(pageNumber, pageSize);

            return View(viewModel);
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

            // 2. Lấy từ khóa (ví dụ: "Loa" từ "Loa Bluetooth X5")
            //    Chúng ta giả định từ đầu tiên của tên SP là từ khóa chính.
            string keyword = sanpham.TenSanPham.Split(' ')[0];

            // 3. Lấy 4 sản phẩm LIÊN QUAN (thử tìm theo từ khóa trước)
            var relatedProducts = await _context.Sanphams
                .Where(s => s.MaDanhMuc == sanpham.MaDanhMuc && // Phải cùng danh mục
                            s.MaSanPham != sanpham.MaSanPham && // Phải khác sản phẩm hiện tại
                            s.TenSanPham.Contains(keyword))     // <-- Tên phải chứa từ khóa
                .Take(4) // Chỉ lấy 4 sản phẩm
                .ToListAsync();

            // 4. (Dự phòng) Nếu không tìm thấy (ví dụ: sản phẩm tên "JBL X5")
            //    thì quay lại cách cũ (lấy bất kỳ 4 SP cùng danh mục)
            if (!relatedProducts.Any())
            {
                relatedProducts = await _context.Sanphams
                    .Where(s => s.MaDanhMuc == sanpham.MaDanhMuc &&
                                s.MaSanPham != sanpham.MaSanPham)
                    .Take(4)
                    .ToListAsync();
            }

            // 5. Gửi danh sách liên quan qua ViewData (giống như cũ)
            ViewData["RelatedProducts"] = relatedProducts;

            // Gửi 1 đối tượng "sanpham" duy nhất này đến View
            return View(sanpham);
        }
        // Trong ProductController.cs
        // ...
        // Sửa lỗi NullReferenceException khi truy cập MaDanhMucNavigation
        public IActionResult Category(int id, int? page)
        {
            int pageSize = 12;
            int pageNumber = (page ?? 1);

            var productsQuery = _context.Sanphams
                                        .Include(s => s.MaDanhMucNavigation) // <-- THÊM DÒNG NÀY
                                        .Where(s => s.MaDanhMuc == id)
                                        .OrderByDescending(s => s.MaSanPham)
                                        .AsQueryable(); // Chuyển thành IQueryable

            var products = productsQuery.ToPagedList(pageNumber, pageSize);

            // Lấy Tên danh mục. Phải kiểm tra tồn tại và truy vấn lại Danh mục nếu products không có phần tử
            if (products.Any())
            {
                ViewData["Title"] = products.First().MaDanhMucNavigation.TenDanhMuc;
            }
            else
            {
                // Nếu danh sách sản phẩm trống, ta truy vấn riêng để lấy tên Danh mục
                var category = _context.Danhmucs.FirstOrDefault(d => d.MaDanhMuc == id);
                ViewData["Title"] = category != null ? category.TenDanhMuc : "Danh mục không tồn tại";
            }

            ViewData["CategoryId"] = id;

            // QUAN TRỌNG: Dùng View "Index" (vì nó có PagedList và cấu trúc giao diện chung)
            return View("Index", products);
        }
        public IActionResult Search(string query, int? page) // Bỏ "async Task"
        {
            ViewData["Query"] = query;
            int pageSize = 12;
            int pageNumber = (page ?? 1);

            var queryableProducts = _context.Sanphams
                                            .Include(s => s.MaDanhMucNavigation)
                                            .AsQueryable();

            if (string.IsNullOrEmpty(query))
            {
                ViewData["Title"] = "Tất cả sản phẩm";
            }
            else
            {
                ViewData["Title"] = $"Kết quả tìm kiếm cho: \"{query}\"";
                var queryParts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var part in queryParts)
                {
                    var likePattern = $"%{part}%";
                    queryableProducts = queryableProducts.Where(
                        s => EF.Functions.Like(s.TenSanPham, likePattern)
                    );
                }
            }

            // Bỏ "await", đổi sang .ToPagedList()
            var products = queryableProducts
                                 .OrderByDescending(s => s.MaSanPham)
                                 .ToPagedList(pageNumber, pageSize); // <-- SỬA Ở ĐÂY

            return View("Index", products);
        }
    }
}