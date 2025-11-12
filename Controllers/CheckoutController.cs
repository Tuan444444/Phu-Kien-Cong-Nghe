using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuKienCongNghe.Data;      // DbContext của bạn
using PhuKienCongNghe.Extensions;  // SessionExtensions
using PhuKienCongNghe.Models;      // Models (Donhang, Chitietdonhang)
using PhuKienCongNghe.ViewModels; // ViewModels
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims; // Dùng để lấy ID người dùng
using System.Threading.Tasks;

namespace PhuKienCongNghe.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly PhukiencongngheDbContext _context;

        public const string CHECKOUT_CART_KEY = "CheckoutCart"; //Key giỏ hàng con để thanh toán
        public CheckoutController(PhukiencongngheDbContext context)
        {
            _context = context;
        }

        // === HÀM NỘI BỘ: Lấy giỏ hàng từ Session ===
        private List<CartItemViewModel> GetCart()
        {
            return HttpContext.Session.Get<List<CartItemViewModel>>(CartController.CARTKEY)
                   ?? new List<CartItemViewModel>();
        }
        // HÀM NỘI BỘ MỚI: Lấy giỏ hàng SẼ thanh toán
        private List<CartItemViewModel> GetCheckoutCart()
        {
            return HttpContext.Session.Get<List<CartItemViewModel>>(CHECKOUT_CART_KEY) ?? new List<CartItemViewModel>();
        }
        private void SaveCartSession(List<CartItemViewModel> cart)
        {
            // Dùng hàm "Set" từ file SessionExtensions.cs của bạn
            HttpContext.Session.Set(CartController.CARTKEY, cart);
            // Tính tổng SỐ LƯỢNG (Quantity) của tất cả items
            int totalQuantity = cart.Sum(item => item.SoLuong);
            // Lưu tổng số lượng vào một Session key MỚI
            HttpContext.Session.SetInt32("CartCount", totalQuantity);
        }

        // --- CÁC HÀM ACTION CHÍNH ---
        //Get: /Checkout/Index/ids=1&ids=5
        //Hiển thị trang thanh toán duy nhất
        [HttpGet]
        public IActionResult Index([FromQuery] List<int> ids)
        {
            // Kiểm tra đăng nhập
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Request.Path + Request.QueryString });
            }
            List<CartItemViewModel> checkoutCart;

            if (ids != null && ids.Count > 0)
            {
                // 2. Lọc giỏ hàng chính dựa trên 'ids'
                var mainCart = GetCart(); // Lấy giỏ hàng TỔNG
                checkoutCart = mainCart.Where(item => ids.Contains(item.MaSanPham)).ToList();   
                // 3. Lưu giỏ hàng con này vào Session MỚI
                HttpContext.Session.Set(CHECKOUT_CART_KEY, checkoutCart);
            }
            else
            {
                // 4. Xử lý trường hợp F5 (tải lại trang)
                // Thử lấy giỏ hàng con từ Session
                checkoutCart = HttpContext.Session.Get<List<CartItemViewModel>>(CHECKOUT_CART_KEY) ?? new List<CartItemViewModel>();
            }

            // 5. Kiểm tra giỏ hàng con (thay vì giỏ hàng chính)
            if (checkoutCart.Count == 0)
            {
                // Nếu không có gì để thanh toán, quay về giỏ hàng chính
                TempData["ErrorMessage"] = "Bạn chưa chọn sản phẩm để thanh toán.";
                return RedirectToAction("Index", "Cart");
            }

            // 3. Tạo ViewModel để hiển thị
            var model = new CheckoutViewModel
            {
                CartItems = checkoutCart,
                TongTien = checkoutCart.Sum(item => item.ThanhTien)
            };

            // Lấy thông tin người dùng đã đăng nhập và điền sẵn
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var user = _context.Nguoidungs.Find(int.Parse(userId));
                if (user != null)
                {
                    model.HoTenNguoiNhan = user.HoTen;
                    model.SoDienThoai = user.SoDienThoai;
                    model.Email = user.Email;
                }
            }

            return View(model); // Views/Checkout/Index.cshtml
        }

        //POST: Checkout/ConfirmOder
        //Được gọi khi nhấn xác nhận đặt hàng từ pop-up
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOrder(CheckoutViewModel model)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            //Lấy giỏ hàng từ session
            var cart = GetCheckoutCart();
            if (cart.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }
            // Gán lại giỏ hàng vào model (vì nó không được post về)
            model.CartItems = cart;
            model.TongTien = cart.Sum(item => item.ThanhTien);

            // Kiểm tra validation (SĐT, Tên, Địa chỉ...)
            if (!ModelState.IsValid)
            {
                // Nếu lỗi, trả về View Index và hiển thị lỗi validation
                return View("Index", model);
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Chỉ xử lý COD
            if (model.PaymentMethod == "COD")
            {
                return await ProcessCOD(model, userId, cart);
            }

            // Các phương thức khác quay về trang Index
            ModelState.AddModelError(string.Empty, "Phương thức thanh toán không hợp lệ.");
            return View("Index", model);

        }

        private async Task<IActionResult> ProcessCOD(CheckoutViewModel model, int userId, List<CartItemViewModel> cart)
        {
            // (Kiểm tra null trước)
            if (string.IsNullOrEmpty(model.HoTenNguoiNhan) || string.IsNullOrEmpty(model.SoDienThoai))
            {
                ModelState.AddModelError(string.Empty, "Thông tin người nhận bị thiếu.");
                return View("Index", model);
            }
            double tongTienDonHang = cart.Sum(item => item.ThanhTien);
            // Dùng Transaction để đảm bảo an toàn dữ liệu
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var donHang = new Donhang
                    {
                        MaNguoiDung = userId,
                        NgayDat = DateTime.Now,
                        HoTenNguoiNhan = model.HoTenNguoiNhan,
                        SDTNguoiNhan = model.SoDienThoai,
                        DiaChiNhanHang = $"{model.DiaChiCuThe}, {model.PhuongXa}, {model.QuanHuyen}, {model.TinhThanh}",
                        TongTien = tongTienDonHang,
                        TrangThai = "Chờ xử lý" // Trạng thái mặc định
                    };

                    _context.Donhangs.Add(donHang);
                    await _context.SaveChangesAsync();

                    foreach (var item in cart)
                    {
                        var chiTiet = new Chitietdonhang
                        {
                            MaDonHang = donHang.MaDonHang,
                            MaSanPham = item.MaSanPham,
                            SoLuong = item.SoLuong,
                            DonGia = item.Gia
                        };
                        _context.Chitietdonhangs.Add(chiTiet);

                        var sanPham = await _context.Sanphams.FindAsync(item.MaSanPham);
                        if (sanPham != null && sanPham.SoLuongTon >= item.SoLuong)
                        {
                            sanPham.SoLuongTon -= item.SoLuong;
                        }
                        else
                        {
                            throw new Exception($"Sản phẩm {item.TenSanPham} không đủ số lượng.");
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // 5. Lấy giỏ hàng CHÍNH
                    var mainCart = GetCart();

                    // 6. Lấy giỏ hàng ĐÃ MUA (là biến 'cart' được truyền vào)
                    // và xóa các sản phẩm đã mua khỏi giỏ hàng CHÍNH
                    mainCart.RemoveAll(item => cart.Any(p => p.MaSanPham == item.MaSanPham));

                    // 7. Lưu lại giỏ hàng CHÍNH (đã được cập nhật)
                    SaveCartSession(mainCart);

                    // 8. Xóa các Session của checkout
                    HttpContext.Session.Remove(CHECKOUT_CART_KEY);

                    // 9. Chuyển đến trang thành công
                    return RedirectToAction("OrderSuccess");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    var message = ex.Message;
                    if (ex.InnerException != null)
                    {
                        message += " | INNER: " + ex.InnerException.Message;
                    }

                    ModelState.AddModelError(string.Empty, $"Lỗi khi đặt hàng: {message}");
                    return View("Index", model);
                }

            }
        }

        public IActionResult OrderSuccess()
        {
            return View("OrderSuccess");
        }
    }
}
