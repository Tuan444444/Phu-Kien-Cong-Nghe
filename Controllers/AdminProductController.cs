using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PhuKienCongNghe.Data;
using PhuKienCongNghe.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
//using Microsoft.AspNetCore.Authorization;
using PhuKienCongNghe.ViewModels; 
using PhuKienCongNghe.Services;
namespace PhuKienCongNghe.Controllers
{
  //  [Authorize(Roles = "admin")]
    public class AdminProductController : Controller
    {
        private readonly PhukiencongngheDbContext _context;
        private readonly FeaturedProductService _featuredService;

        public AdminProductController(PhukiencongngheDbContext context, FeaturedProductService featuredService)
        {
            _context = context;
            _featuredService = featuredService;
        }

        public async Task<IActionResult> Index(

            string? searchString,   // 1. Tham số cho Tên (Tìm kiếm)
            int? categoryId,        // 2. Tham số cho Danh mục (Lọc)
            string? priceRange      // 3. Tham số cho Giá (Lọc)
        )
        {
            // --- A. LẤY DỮ LIỆU CHO CÁC HỘP LỌC (DROPDOWN) ---

            // Lấy tất cả danh mục để hiển thị trong dropdown
            // Dùng SelectList, tham số thứ 4 (categoryId) sẽ tự động chọn giá trị cũ
            ViewBag.Categories = new SelectList(
                await _context.Danhmucs.OrderBy(d => d.TenDanhMuc).ToListAsync(),
                "MaDanhMuc",
                "TenDanhMuc",
                categoryId
            );

            // Lưu lại các giá trị lọc cũ để hiển thị lại trên View
            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentCategory = categoryId;
            ViewBag.CurrentPriceRange = priceRange;

            // --- B. XÂY DỰNG TRUY VẤN (QUERY) ĐỘNG ---

            // 1. Bắt đầu với một truy vấn cơ bản (IQueryable)
            var products = _context.Sanphams
                .Include(s => s.MaDanhMucNavigation)
                .AsQueryable(); // AsQueryable() rất quan trọng

            // 2. Lọc theo Tên (Search String)
            if (!String.IsNullOrEmpty(searchString))
            {
                products = products.Where(s => s.TenSanPham.Contains(searchString));
            }

            // 3. Lọc theo Danh mục (Category ID)
            if (categoryId.HasValue && categoryId > 0)
            {
                products = products.Where(s => s.MaDanhMuc == categoryId);
            }

            // 4. Lọc theo Khoảng giá (Price Range)
            if (!String.IsNullOrEmpty(priceRange))
            {
                switch (priceRange)
                {
                    case "1": // Dưới 500.000
                        products = products.Where(s => s.Gia < 500000);
                        break;
                    case "2": // 500.000 - 1.000.000
                        products = products.Where(s => s.Gia >= 500000 && s.Gia <= 1000000);
                        break;
                    case "3": // Trên 1.000.000
                        products = products.Where(s => s.Gia > 1000000);
                        break;
                }
            }

            // --- C. THỰC THI TRUY VẤN VÀ TRẢ VỀ VIEW ---

            // Sắp xếp và thực thi truy vấn (ToListAsync)
            var model = await products.OrderByDescending(s => s.MaSanPham).ToListAsync();

            return View(model);
        }

        //
        // 2. CREATE (GET - Hiển thị Form)
        //
        public IActionResult Create()
        {
            // Gửi danh sách Danh Mục cho View (để làm dropdown)
            ViewBag.MaDanhMuc = new SelectList(_context.Danhmucs, "MaDanhMuc", "TenDanhMuc");
            return View();
        }

        //
        // 2. CREATE (POST - Lưu Form)
        //
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TenSanPham,MoTa,Gia,SoLuongTon,HinhAnh,MaDanhMuc")] Sanpham sanpham)
        {
            ModelState.Remove("MaDanhMucNavigation");
            if (ModelState.IsValid)
            {
                // TODO: Xử lý Upload file cho HinhAnh (hiện tại đang là nhập link)

                _context.Add(sanpham);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index)); // Quay về trang danh sách
            }

            // Nếu lỗi, gửi lại danh sách Danh Mục
            ViewBag.MaDanhMuc = new SelectList(_context.Danhmucs, "MaDanhMuc", "TenDanhMuc", sanpham.MaDanhMuc);
            return View(sanpham);
        }

        //
        // 3. EDIT (GET - Hiển thị Form)
        //
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var sanpham = await _context.Sanphams.FindAsync(id);
            if (sanpham == null) return NotFound();

            // Gửi danh sách Danh Mục, và chọn sẵn danh mục cũ
            ViewBag.MaDanhMuc = new SelectList(_context.Danhmucs, "MaDanhMuc", "TenDanhMuc", sanpham.MaDanhMuc);
            return View(sanpham);
        }

        //
        // 3. EDIT (POST - Lưu Form)
        //
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaSanPham,TenSanPham,MoTa,Gia,SoLuongTon,HinhAnh,MaDanhMuc")] Sanpham sanpham)
        {
            if (id != sanpham.MaSanPham) return NotFound();

            ModelState.Remove("MaDanhMucNavigation");

            if (ModelState.IsValid)
            {
                try
                {
                    // TODO: Xử lý Upload file nếu HinhAnh thay đổi

                    _context.Update(sanpham);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Sanphams.Any(e => e.MaSanPham == sanpham.MaSanPham))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index)); // Quay về trang danh sách
            }

            ViewBag.MaDanhMuc = new SelectList(_context.Danhmucs, "MaDanhMuc", "TenDanhMuc", sanpham.MaDanhMuc);
            return View(sanpham);
        }

        //
        // 4. DELETE (POST - Xử lý Xóa)
        //
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sanpham = await _context.Sanphams.FindAsync(id);
            if (sanpham != null)
            {
                _context.Sanphams.Remove(sanpham);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // 5. PROMOTIONS (GET - Mở trang "Set Giá")
        //
        [HttpGet]
        public async Task<IActionResult> Promotions()
        {
            var viewModel = new AdminFeaturedViewModel();

            var featuredEntries = _featuredService.GetAll();
            var featuredIds = featuredEntries.Select(f => f.MaSanPham).ToList();

            var sanphamsDictionary = await _context.Sanphams
                .Where(p => featuredIds.Contains(p.MaSanPham))
                .ToDictionaryAsync(p => p.MaSanPham);

            foreach (var entry in featuredEntries)
            {
                if (sanphamsDictionary.TryGetValue(entry.MaSanPham, out var sanpham))
                {
                    entry.Sanpham = sanpham;
                }
            }
            viewModel.FeaturedProducts = featuredEntries.Where(e => e.Sanpham != null).ToList();

            var productsToFeature = await _context.Sanphams
                .Where(p => !featuredIds.Contains(p.MaSanPham))
                .OrderBy(p => p.TenSanPham)
                .ToListAsync();

            viewModel.ProductsToFeature = new SelectList(productsToFeature, "MaSanPham", "TenSanPham");

            // Gửi "bảng giá" của các SP trong dropdown ra View
            ViewData["ProductPriceJson"] = JsonSerializer.Serialize(
                productsToFeature.Select(p => new { p.MaSanPham, p.Gia })
            );
            return View(viewModel);
        }

        //
        // 6. ADD PROMOTION (POST - Thêm Khuyến mãi)
        //
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddPromotion(AdminFeaturedViewModel model)
        {
            if (model.NewFeaturedProductId > 0 && model.NewFeaturedPrice > 0)
            {
                var newFeatured = new SanPhamNoiBat
                {
                    MaSanPham = model.NewFeaturedProductId,
                    GiaKhuyenMai = model.NewFeaturedPrice
                };
                _featuredService.Add(newFeatured);
                TempData["ToastMessage"] = "Thêm khuyến mãi thành công!";
            }
            else
            {
                TempData["ToastError"] = "Thêm thất bại, vui lòng chọn SP và nhập giá.";
            }
            return RedirectToAction("Promotions");
        }

        //
        // 7. DELETE PROMOTION (POST - Xóa Khuyến mãi)
        //
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePromotion(int id)
        {
            _featuredService.Delete(id);
            TempData["ToastMessage"] = "Đã gỡ sản phẩm khỏi khuyến mãi.";
            return RedirectToAction("Promotions");
        }
    }
}
