using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhuKienCongNghe.Data;      // DbContext của bạn
using PhuKienCongNghe.Extensions;  // SessionExtensions
using PhuKienCongNghe.Models;      // Models (Donhang, Chitietdonhang)
using PhuKienCongNghe.ViewModels; // ViewModels
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims; // Dùng để lấy ID người dùng
using System.Threading.Tasks;

namespace PhuKienCongNghe.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly PhukiencongngheDbContext _context;

        public CheckoutController(PhukiencongngheDbContext context)
        {
            _context = context;
        }

        // HÀM NỘI BỘ: Lấy giỏ hàng từ Session
        private List<CartItemViewModel> GetCart()
        {
            return HttpContext.Session.Get<List<CartItemViewModel>>(CartController.CARTKEY) ?? new List<CartItemViewModel>();
        }

        // GET: /Checkout/Index
        [HttpGet]
        public IActionResult Index()
        {
            // === KIỂM TRA ĐĂNG NHẬP ===
            if (!User.Identity.IsAuthenticated)
            {
                // Nếu chưa đăng nhập, bắt chuyển đến trang đăng nhập
                // và truyền URL trang này để quay lại sau khi đăng nhập thành công
                return RedirectToAction("Login", "Account", new { returnUrl = "/Checkout/Index" });
            }

            var cart = GetCart();
            if (cart.Count == 0)
            {
                // Giỏ hàng rỗng, không cho thanh toán
                return RedirectToAction("Index", "Cart");
            }

            var viewModel = new CheckoutViewModel
            {
                CartItems = cart,
                TongTien = cart.Sum(item => item.ThanhTien)
            };

            // Tự động điền thông tin người dùng nếu họ đã đăng nhập
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _context.Nguoidungs.Find(int.Parse(userId)); // Tìm người dùng
            if (user != null)
            {
                viewModel.HoTenNguoiNhan = user.HoTen;
                viewModel.SoDienThoai = user.SoDienThoai;
                viewModel.Email = user.Email;
                // Bạn có thể điền địa chỉ mặc định của họ nếu có
            }

            return View(viewModel);
        }

        // POST: /Checkout/Index
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CheckoutViewModel model)
        {
            // === KIỂM TRA LẠI ĐĂNG NHẬP ===
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = GetCart();
            if (cart.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            // Gán lại giỏ hàng và tổng tiền vào model (vì nó không được POST về)
            model.CartItems = cart;
            model.TongTien = cart.Sum(item => item.ThanhTien);

            if (!ModelState.IsValid)
            {
                // Nếu dữ liệu form không hợp lệ, trả về View và hiển thị lỗi
                return View(model);
            }

            // Lấy MaNguoiDung từ user đã đăng nhập
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                // Lỗi không tìm thấy ID người dùng (dù đã đăng nhập)
                ModelState.AddModelError(string.Empty, "Lỗi xác thực người dùng.");
                return View(model);
            }

            // Bắt đầu xử lý đơn hàng
            if (model.PaymentMethod == "COD")
            {
                // Xử lý Thanh toán khi nhận hàng (COD)
                return await ProcessCOD(model, userId, cart);
            }
            else if (model.PaymentMethod == "Online")
            {
                // Xử lý Thanh toán trực tuyến (VNPAY, MoMo...)
                // Tạm thời chuyển hướng sang một action xử lý riêng
                return RedirectToAction("ProcessOnlinePayment", new { amount = model.TongTien });
            }

            return View(model);
        }

        private async Task<IActionResult> ProcessCOD(CheckoutViewModel model, int userId, List<CartItemViewModel> cart)
        {
            // Dùng Transaction để đảm bảo an toàn dữ liệu
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Tạo đối tượng Donhang
                    var donHang = new Donhang
                    {
                        MaNguoiDung = userId,
                        NgayDat = DateTime.Now,
                        HoTenNguoiNhan = model.HoTenNguoiNhan,
                        SDTNguoiNhan = model.SoDienThoai,
                        DiaChiNhanHang = $"{model.DiaChiCuThe}, {model.PhuongXa}, {model.QuanHuyen}, {model.TinhThanh}",
                        TongTien = model.TongTien,
                        TrangThai = "Chờ xử lý" // Trạng thái mặc định
                    };

                    _context.Donhangs.Add(donHang);
                    await _context.SaveChangesAsync(); // Lưu để lấy MaDonHang

                    // 2. Thêm các Chitietdonhang
                    foreach (var item in cart)
                    {
                        var chiTiet = new Chitietdonhang
                        {
                            MaDonHang = donHang.MaDonHang,
                            MaSanPham = item.MaSanPham,
                            SoLuong = item.SoLuong,
                            DonGia = item.Gia // Lưu đơn giá tại thời điểm mua
                        };
                        _context.Chitietdonhangs.Add(chiTiet);

                        // 3. (Quan trọng) Trừ số lượng tồn kho
                        var sanPham = await _context.Sanphams.FindAsync(item.MaSanPham);
                        if (sanPham != null && sanPham.SoLuongTon >= item.SoLuong)
                        {
                            sanPham.SoLuongTon -= item.SoLuong;
                        }
                        else
                        {
                            // Nếu hết hàng, hủy transaction
                            throw new Exception($"Sản phẩm {item.TenSanPham} không đủ số lượng.");
                        }
                    }

                    await _context.SaveChangesAsync(); // Lưu CTHĐ và cập nhật tồn kho

                    // 4. Commit transaction
                    await transaction.CommitAsync();

                    // 5. Xóa giỏ hàng khỏi Session
                    HttpContext.Session.Remove(CartController.CARTKEY);
                    HttpContext.Session.Remove("CartCount"); // Xóa cả bộ đếm

                    // 6. Chuyển đến trang thành công
                    return RedirectToAction("OrderSuccess");
                }
                catch (Exception ex)
                {
                    // Nếu có lỗi, rollback tất cả
                    await transaction.RollbackAsync();
                    ModelState.AddModelError(string.Empty, $"Lỗi khi đặt hàng: {ex.Message}. Vui lòng thử lại.");
                    return View("Index", model); // Quay lại trang Index với model và lỗi
                }
            }
        }

        // GET: /Checkout/ProcessOnlinePayment
        public IActionResult ProcessOnlinePayment(double amount)
        {
            // TODO: Tích hợp VNPAY/MoMo tại đây
            // 1. Tạo một đơn hàng trong CSDL với trạng thái "Chờ thanh toán"
            // 2. Gọi API của cổng thanh toán (VNPAY...)
            // 3. Nhận về URL thanh toán
            // 4. Redirect người dùng đến URL đó

            // Tạm thời, chúng ta sẽ trả về một View thông báo
            TempData["PaymentAmount"] = amount;
            return View("OnlinePaymentPending"); // Tạo View này
        }


        // GET: /Checkout/OrderSuccess
        public IActionResult OrderSuccess()
        {
            return View("OderSuccess"); // Tạo View này
        }
    }
}