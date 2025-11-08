using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuKienCongNghe.Data;
using PhuKienCongNghe.Models;
using System.Linq;
using System.Threading.Tasks;
// using Microsoft.AspNetCore.Authorization;
namespace PhuKienCongNghe.Controllers
{
    public class AdminCategoriesController : Controller
    {
        private readonly PhukiencongngheDbContext _context;

        public AdminCategoriesController(PhukiencongngheDbContext context)
        {
            _context = context;
        }

        //
        // GET: /AdminCategories/Index
        // Trang này sẽ hiển thị CẢ Form Thêm Mới VÀ Bảng Liệt Kê
        //
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Quản lý Danh mục";
            var categories = await _context.Danhmucs
                                .OrderBy(d => d.TenDanhMuc)
                                .ToListAsync();
            return View(categories);
        }

        //
        // POST: /AdminCategories/Create
        // Xử lý logic Thêm Mới (từ Form trên trang Index)
        //
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TenDanhMuc")] Danhmuc danhmuc)
        {
            if (ModelState.IsValid)
            {
                _context.Add(danhmuc);
                await _context.SaveChangesAsync();
                TempData["ToastMessage"] = "Thêm danh mục thành công!";
            }
            else
            {
                TempData["ToastError"] = "Thêm thất bại. Tên danh mục không được để trống.";
            }
            return RedirectToAction(nameof(Index));
        }

        //
        // GET: /AdminCategories/Edit/5
        // Trang Sửa (Form Sửa)
        //
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var danhmuc = await _context.Danhmucs.FindAsync(id);
            if (danhmuc == null)
            {
                return NotFound();
            }
            ViewData["Title"] = "Chỉnh sửa Danh mục";
            return View(danhmuc);
        }

        //
        // POST: /AdminCategories/Edit/5
        // Xử lý logic Sửa
        //
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaDanhMuc,TenDanhMuc")] Danhmuc danhmuc)
        {
            if (id != danhmuc.MaDanhMuc)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(danhmuc);
                    await _context.SaveChangesAsync();
                    TempData["ToastMessage"] = "Cập nhật danh mục thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Danhmucs.Any(e => e.MaDanhMuc == danhmuc.MaDanhMuc))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(danhmuc);
        }

        //
        // POST: /AdminCategories/Delete/5
        // Xử lý logic Xóa (từ Nút Xóa trên trang Index)
        //
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // 1. Tải danh mục CÙNG VỚI (Include) danh sách sản phẩm của nó
            var danhmuc = await _context.Danhmucs
                .Include(d => d.Sanphams) // <-- Quan trọng: Tải các sản phẩm liên quan
                .FirstOrDefaultAsync(d => d.MaDanhMuc == id);

            if (danhmuc == null)
            {
                TempData["ToastError"] = "Không tìm thấy danh mục.";
                return RedirectToAction(nameof(Index));
            }

            // 2. KIỂM TRA (ĐÂY LÀ PHẦN CẢNH BÁO CỦA BẠN)
            // Nếu danh sách sản phẩm .Sanphams có chứa bất kỳ
            if (danhmuc.Sanphams.Any())
            {
                // 3. Gửi cảnh báo LỖI và DỪNG LẠI
                TempData["ToastError"] = "Xóa thất bại! Vẫn còn sản phẩm trong danh mục này.";
                return RedirectToAction(nameof(Index));
            }

            // 4. Nếu không có sản phẩm (an toàn), TIẾN HÀNH XÓA
            _context.Danhmucs.Remove(danhmuc);
            await _context.SaveChangesAsync();
            TempData["ToastMessage"] = "Xóa danh mục thành công!";

            return RedirectToAction(nameof(Index));
        }
    }
}
