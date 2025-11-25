using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuKienCongNghe.Data;
using PhuKienCongNghe.Models;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList.Extensions;
using PhuKienCongNghe.Services;
using PhuKienCongNghe.ViewModels;
using X.PagedList;
using System.Collections.Generic;
using System;

namespace PhuKienCongNghe.Controllers
{
    public class ProductController : Controller
    {
        private readonly PhukiencongngheDbContext _context;
        private readonly FeaturedProductService _featuredService;

        public ProductController(PhukiencongngheDbContext context, FeaturedProductService featuredService)
        {
            _context = context;
            _featuredService = featuredService;
        }

        // ==========================================
        // 1. TRANG CHỦ CỬA HÀNG (HIỂN THỊ TẤT CẢ)
        // ==========================================
        public IActionResult Index(int? page)
        {
            ViewData["Title"] = "Tất cả sản phẩm";

            // Các tham số quan trọng để Phân trang & Menu hoạt động đúng
            ViewData["CurrentAction"] = "Index";
            ViewData["CategoryId"] = null;
            ViewData["Query"] = null;

            var viewModel = new ShopViewModel();

            // --- A. LẤY SẢN PHẨM NỔI BẬT ---
            var featuredEntries = _featuredService.GetAll().Take(8).ToList();
            if (featuredEntries.Any())
            {
                var featuredIds = featuredEntries.Select(f => f.MaSanPham).ToList();
                var sanphamsDictionary = _context.Sanphams
                    .Where(p => featuredIds.Contains(p.MaSanPham))
                    .ToDictionary(p => p.MaSanPham);

                foreach (var entry in featuredEntries)
                {
                    if (sanphamsDictionary.TryGetValue(entry.MaSanPham, out var sanpham))
                    {
                        entry.Sanpham = sanpham;
                    }
                }
                viewModel.FeaturedProducts = featuredEntries.Where(e => e.Sanpham != null).ToList();
            }
            else
            {
                // Trang 2 trở đi không cần hiện lại bảng xếp hạng này
                viewModel.BestSellers = new List<Sanpham>();
            }

            // --- B. SẢN PHẨM BÁN CHẠY (MỚI THÊM) ---
            // Chỉ tính toán khi ở trang chủ (page = 1 hoặc null) để tối ưu hiệu năng
            if (page == null || page == 1)
            {
                // 1. Lấy Top 7 ID sản phẩm bán chạy nhất
                // Tại sao lấy 7? Để hiển thị: 1 sản phẩm TOP 1 to đùng + 6 sản phẩm nhỏ bên cạnh (2 hàng x 3 cột) -> Đẹp đội hình.
                var topSellingIds = _context.Chitietdonhangs
                    .GroupBy(ct => ct.MaSanPham)
                    .Select(g => new {
                        MaSanPham = g.Key,
                        TongBan = g.Sum(x => x.SoLuong)
                    })
                    .OrderByDescending(x => x.TongBan)
                    .Take(7) // <--- SỬA THÀNH 7 ĐỂ KHỚP GIAO DIỆN
                    .Select(x => x.MaSanPham)
                    .ToList();

                // 2. Lấy thông tin chi tiết sản phẩm từ bảng Sanpham
                var bestSellers = _context.Sanphams
                    .Where(p => topSellingIds.Contains(p.MaSanPham))
                    .ToList();

                // 3. Sắp xếp lại danh sách theo đúng thứ tự bán chạy (Top 1 lên đầu)
                // (Vì lệnh Where bên trên có thể làm xáo trộn thứ tự)
                viewModel.BestSellers = bestSellers
                    .OrderBy(p => topSellingIds.IndexOf(p.MaSanPham))
                    .ToList();
            }
            else
            {
                // Trang 2 trở đi không cần hiện lại bảng xếp hạng này
                viewModel.BestSellers = new List<Sanpham>();
            }
          

            // --- B. LẤY TẤT CẢ SẢN PHẨM ---
            int pageSize = 12;
            int pageNumber = (page ?? 1);

            var allProducts = _context.Sanphams
                                    .Include(s => s.MaDanhMucNavigation)
                                    .OrderByDescending(s => s.MaSanPham);

            viewModel.AllProducts = allProducts.ToPagedList(pageNumber, pageSize);

            return View(viewModel);
        }

        // ==========================================
        // 2. LỌC THEO DANH MỤC (CATEGORY)
        // ==========================================
        public async Task<IActionResult> Category(int id, int? page)
        {
ViewData["CurrentAction"] = "Category"; // Quan trọng: Để phân trang biết đường dẫn
            ViewData["CategoryId"] = id;            // Quan trọng: Để giữ ID danh mục khi sang trang 2
            ViewData["Query"] = null;

            var category = await _context.Danhmucs.FindAsync(id);
            ViewData["Title"] = (category != null) ? category.TenDanhMuc : "Sản phẩm";

            var viewModel = new ShopViewModel();
            viewModel.FeaturedProducts = new List<SanPhamNoiBat>(); // Danh mục ko hiện SP nổi bật

            int pageSize = 12;
            int pageNumber = (page ?? 1);

            // Lọc sản phẩm
            var products = _context.Sanphams
                                   .Include(s => s.MaDanhMucNavigation)
                                   .Where(s => s.MaDanhMuc == id)
                                   .OrderByDescending(s => s.MaSanPham);

            viewModel.AllProducts = products.ToPagedList(pageNumber, pageSize);

            return View("Index", viewModel);
        }

        // ==========================================
        // 3. TÌM KIẾM SẢN PHẨM (SEARCH) - ĐÃ SỬA LẠI CHUẨN
        // ==========================================
        public IActionResult Search(string query, int? page)
        {
            ViewData["CurrentAction"] = "Search"; // Quan trọng: Để phân trang biết là đang tìm kiếm
            ViewData["CategoryId"] = null;
            ViewData["Query"] = query;            // Quan trọng: Để giữ từ khóa khi sang trang 2

            var viewModel = new ShopViewModel();
            viewModel.FeaturedProducts = new List<SanPhamNoiBat>();

            int pageSize = 12;
            int pageNumber = (page ?? 1);

            var queryableProducts = _context.Sanphams
                                            .Include(s => s.MaDanhMucNavigation)
                                            .AsQueryable();

            if (!string.IsNullOrEmpty(query))
            {
                ViewData["Title"] = $"Kết quả tìm kiếm: \"{query}\"";

                var queryParts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var part in queryParts)
                {
                    var likePattern = $"%{part}%";
                    queryableProducts = queryableProducts.Where(
                        s => EF.Functions.Like(s.TenSanPham, likePattern)
                    );
                }
            }
            else
            {
                ViewData["Title"] = "Tất cả sản phẩm";
            }

            // Đổ dữ liệu vào ViewModel (Chứ ko trả về List trực tiếp nữa)
            viewModel.AllProducts = queryableProducts
                                    .OrderByDescending(s => s.MaSanPham)
                                    .ToPagedList(pageNumber, pageSize);

            return View("Index", viewModel);
        }
// ==========================================
        // 4. CHI TIẾT SẢN PHẨM
        // ==========================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var sanpham = await _context.Sanphams
                .Include(s => s.MaDanhMucNavigation)
                .FirstOrDefaultAsync(m => m.MaSanPham == id);

            if (sanpham == null) return NotFound();

            string keyword = sanpham.TenSanPham.Split(' ')[0];

            var relatedProducts = await _context.Sanphams
                .Where(s => s.MaDanhMuc == sanpham.MaDanhMuc &&
                            s.MaSanPham != sanpham.MaSanPham &&
                            s.TenSanPham.Contains(keyword))
                .Take(4)
                .ToListAsync();

            if (!relatedProducts.Any())
            {
                relatedProducts = await _context.Sanphams
                    .Where(s => s.MaDanhMuc == sanpham.MaDanhMuc &&
                                s.MaSanPham != sanpham.MaSanPham)
                    .Take(4)
                    .ToListAsync();
            }

            ViewData["RelatedProducts"] = relatedProducts;

            return View(sanpham);
        }
    }
}