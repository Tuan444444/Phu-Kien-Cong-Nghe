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
        public const string CHECKOUT_STATE_KEY = "CheckoutState";
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

        // === HÀM NỘI BỘ: Lấy/Tạo trạng thái thanh toán ===
        private CheckoutViewModel GetCheckoutState()
        {
            return HttpContext.Session.Get<CheckoutViewModel>(CHECKOUT_STATE_KEY)
                   ?? new CheckoutViewModel();
        }

        // === HÀM NỘI BỘ: Lưu trạng thái thanh toán ===
        private void SaveCheckoutState(CheckoutViewModel state)
        {
            HttpContext.Session.Set(CHECKOUT_STATE_KEY, state);
        }

        // --- BƯỚC 1: NHẬP ĐỊA CHỈ ---
        [HttpGet]
        public IActionResult Address()
        {
            // Kiểm tra đăng nhập
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = "/Checkout/Address" });
            }

            // Kiểm tra giỏ hàng
            if (GetCart().Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            // Lấy trạng thái (nếu có) hoặc tạo mới
            var model = GetCheckoutState();

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

            return View(model); // Views/Checkout/Address.cshtml
        }

        [HttpPost]
        public IActionResult Address(CheckoutViewModel model)
        {
            var state = GetCheckoutState();

            // Cập nhật thông tin địa chỉ từ form
            state.HoTenNguoiNhan = model.HoTenNguoiNhan;
            state.SoDienThoai = model.SoDienThoai;
            state.Email = model.Email;
            state.TinhThanh = model.TinhThanh;
            state.QuanHuyen = model.QuanHuyen;
            state.PhuongXa = model.PhuongXa;
            state.DiaChiCuThe = model.DiaChiCuThe;
            state.GhiChu = model.GhiChu;

            // Lưu trạng thái vào Session
            SaveCheckoutState(state);

            return RedirectToAction("Payment");
        }

        // --- BƯỚC 2: CHỌN GIAO HÀNG & THANH TOÁN ---
        [HttpGet]
        public IActionResult Payment()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = "/Checkout/Address" });
            }
            if (GetCart().Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            var model = GetCheckoutState();
            if (string.IsNullOrEmpty(model.TinhThanh))
            {
                return RedirectToAction("Address");
            }

            var cart = GetCart();
            model.CartItems = cart;
            model.TongTien = cart.Sum(item => item.ThanhTien);

            return View(model); // Views/Checkout/Payment.cshtml
        }

        [HttpPost]
        public IActionResult Payment(CheckoutViewModel model)
        {
            var state = GetCheckoutState();
            state.PaymentMethod = model.PaymentMethod;
            SaveCheckoutState(state);

            return RedirectToAction("Confirm");
        }

        // --- BƯỚC 3: XÁC NHẬN ĐƠN HÀNG ---
        [HttpGet]
        public IActionResult Confirm()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = "/Checkout/Address" });
            }
            if (GetCart().Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            var model = GetCheckoutState();
            if (string.IsNullOrEmpty(model.PaymentMethod))
            {
                return RedirectToAction("Payment");
            }

            var cart = GetCart();
            model.CartItems = cart;
            model.TongTien = cart.Sum(item => item.ThanhTien);

            return View(model); // Views/Checkout/Confirm.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOrder()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = GetCart();
            var model = GetCheckoutState();
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (cart.Count == 0 || string.IsNullOrEmpty(model.PaymentMethod) || string.IsNullOrEmpty(model.TinhThanh))
            {
                return RedirectToAction("Address");
            }

            if (model.PaymentMethod == "COD")
            {
                return await ProcessCOD(model, userId, cart);
            }
            else
            {
                return RedirectToAction("ProcessOnlinePayment");
            }
        }

        private async Task<IActionResult> ProcessCOD(CheckoutViewModel model, int userId, List<CartItemViewModel> cart)
        {
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
                        TongTien = model.TongTien,
                        TrangThai = "Đang xử lý"
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

                    HttpContext.Session.Remove(CartController.CARTKEY);
                    HttpContext.Session.Remove("CartCount");

                    return RedirectToAction("OrderSuccess");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError(string.Empty, $"Lỗi khi đặt hàng: {ex.Message}");
                    return View("Payment", model);
                }
            }
        }

        public IActionResult Index()
        {
            return RedirectToAction("Address");
        }

        public IActionResult ProcessOnlinePayment(double amount)
        {
            TempData["PaymentAmount"] = amount;
            return View("OnlinePaymentPending");
        }

        public IActionResult OrderSuccess()
        {
            return View("OrderSuccess");
        }
    }
}
