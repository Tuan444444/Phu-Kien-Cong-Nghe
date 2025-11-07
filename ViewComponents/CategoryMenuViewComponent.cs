using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
// Đảm bảo bạn using đúng namespace chứa DbContext và Models
using PhuKienCongNghe.Data;
using PhuKienCongNghe.Models;
using System.Linq;
using System.Threading.Tasks;

namespace PhuKienCongNghe.ViewComponents
{
    // Tên class phải kết thúc bằng "ViewComponent"
    public class CategoryMenuViewComponent : ViewComponent
    {
        // Biến DbContext
        // *** LƯU Ý: Thay "PhukiencongngheDbContext" bằng tên DbContext thật của bạn
        private readonly PhukiencongngheDbContext _context;

        // "Tiêm" DbContext vào
        public CategoryMenuViewComponent(PhukiencongngheDbContext context)
        {
            _context = context;
        }

        // Tên hàm phải là "InvokeAsync"
        // Hàm này sẽ tự động chạy khi được gọi
        public async Task<IViewComponentResult> InvokeAsync()
        {
            // 1. Lấy tất cả danh mục từ Database (dùng file Danhmuc.cs)
            var categories = await _context.Danhmucs.ToListAsync();

            // 2. Trả về View "Default.cshtml" (sẽ tạo ở Bước 2)
            //    và gửi kèm danh sách "categories" làm Model
            return View(categories);
        }
    }
}