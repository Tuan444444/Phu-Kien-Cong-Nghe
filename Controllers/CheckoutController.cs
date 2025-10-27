using Microsoft.AspNetCore.Authorization; // Yêu cầu đăng nhập
using Microsoft.AspNetCore.Mvc;
using PhuKienCongNghe.Data;
using PhuKienCongNghe.Extensions;
using PhuKienCongNghe.Models;
using PhuKienCongNghe.ViewModels;
using System.Security.Claims; // Để lấy MaNguoiDung

namespace PhuKienCongNghe.Controllers
{
    [Authorize] // Bắt buộc phải đăng nhập mới vào được trang này
    public class CheckoutController : Controller
    {
        private readonly PhukiencongngheDbContext _context;
        private const string CartSessionKey = "Cart";

        public CheckoutController(PhukiencongngheDbContext context)
        {
            _context = context;
        }

        // Lấy giỏ hàng (Copy từ CartController)
        private List<CartItemViewModel> GetCart()
        {
            var cart = HttpContext.Session.Get<List<CartItemViewModel>>(CartSessionKey);
            return cart ?? new List<CartItemViewModel>();
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cart = GetCart();
            if (cart.Count == 0)
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống!";
                return RedirectToAction("Index", "Cart");
            }

            // Lấy thông tin người dùng đang đăng nhập
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Nguoidungs.FindAsync(Convert.ToInt32(userId));

            var viewModel = new CheckoutViewModel // Cần tạo ViewModel này
            {
                Cart = new CartViewModel { CartItems = cart, TongTien = cart.Sum(item => item.ThanhTien) },
                HoTenNguoiNhan = user.HoTen,
                SDTNguoiNhan = user.SoDienThoai,
                DiaChiNhanHang = user.DiaChi
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            var cart = GetCart();
            if (cart.Count == 0)
            {
                return RedirectToAction("Index", "Home");
            }

            // Lấy MaNguoiDung từ
            var userId = Convert.ToInt32(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Dùng Transaction để đảm bảo tính toàn vẹn dữ liệu
            // Nếu lưu Đơn hàng lỗi, hoặc lưu Chi Tiết ĐH lỗi, tất cả sẽ bị rollback
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Tạo Đơn hàng
                var donHang = new Donhang
                {
                    MaNguoiDung = userId,
                    NgayDat = DateTime.Now,
                    TongTien = cart.Sum(item => item.ThanhTien),
                    HoTenNguoiNhan = model.HoTenNguoiNhan,
                    SDTNguoiNhan = model.SDTNguoiNhan,
                    DiaChiNhanHang = model.DiaChiNhanHang,
                    TrangThai = "Chờ xử lý" // Giá trị mặc định
                };

                _context.Donhangs.Add(donHang);
                await _context.SaveChangesAsync(); // Lưu để lấy MaDonHang

                // 2. Tạo Chi tiết đơn hàng
                foreach (var item in cart)
                {
                    var chiTiet = new Chitietdonhang
                    {
                        MaDonHang = donHang.MaDonHang, // Lấy ID của đơn hàng vừa tạo
                        MaSanPham = item.MaSanPham,
                        SoLuong = item.SoLuong,
                        DonGia = item.DonGia
                    };
                    _context.Chitietdonhangs.Add(chiTiet);

                    // (Nâng cao) Trừ số lượng tồn kho
                    var product = await _context.Sanphams.FindAsync(item.MaSanPham);
                    if (product != null)
                    {
                        product.SoLuongTon -= item.SoLuong;
                        _context.Sanphams.Update(product);
                    }
                }

                await _context.SaveChangesAsync(); // Lưu tất cả chi tiết và cập nhật tồn kho

                // 3. Commit transaction
                await transaction.CommitAsync();

                // 4. Xóa giỏ hàng
                HttpContext.Session.Remove(CartSessionKey);

                return RedirectToAction("Success", new { id = donHang.MaDonHang });
            }
            catch (Exception)
            {
                // 5. Rollback nếu có lỗi
                await transaction.RollbackAsync();
                TempData["Error"] = "Đã xảy ra lỗi khi đặt hàng. Vui lòng thử lại.";
                return View("Index", model); // Quay lại trang checkout
            }
        }

        public IActionResult Success(int id)
        {
            ViewBag.MaDonHang = id;
            return View();
        }
    }
}