using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuKienCongNghe.Data;
using System.Diagnostics;

namespace PhuKienCongNghe.Controllers
{
    public class HomeController : Controller
    {
        private readonly PhukiencongngheDbContext _context;

        public HomeController(PhukiencongngheDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Lấy 12 sản phẩm mới nhất để hiển thị ra trang chủ
            var products = await _context.Sanphams
                                         .OrderByDescending(p => p.MaSanPham)
                                         .Take(12)
                                         .ToListAsync();
            return View(products);
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}