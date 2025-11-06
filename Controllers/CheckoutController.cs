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

        // HÀM NỘI BỘ: Lấy giỏ hàng từ Session
        private List<CartItemViewModel> GetCart()
        {
            return HttpContext.Session.Get<List<CartItemViewModel>>(CartController.CARTKEY) ?? new List<CartItemViewModel>();
        }
        // HÀM NỘI BỘ: Lấy/Tạo trạng thái thanh toán
        private CheckoutViewModel GetCheckoutState()
        {
            return HttpContext.Session.Get<CheckoutViewModel>(CHECKOUT_STATE_KEY) ?? new CheckoutViewModel();
        }

        // HÀM NỘI BỘ: Lưu trạng thái thanh toán
        private void SaveCheckoutState(CheckoutViewModel state)
        {
            HttpContext.Session.Set(CHECKOUT_STATE_KEY, state);
        }

        // --- BƯỚC 1: NHẬP ĐỊA CHỈ ---

        // GET: /Checkout/Address
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
            var user = _context.Nguoidungs.Find(int.Parse(userId));
            if (user != null)
            {
                model.HoTenNguoiNhan = user.HoTen;
                model.SoDienThoai = user.SoDienThoai;
                model.Email = user.Email;
            }

            return View(model); // Tạo View "Views/Checkout/Address.cshtml"
        }

        // POST: /Checkout/Address
        [HttpPost]
        public IActionResult Address(CheckoutViewModel model)
        {
            // Lấy trạng thái hiện tại (để không mất các thông tin khác)
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

            // Chuyển sang Bước 2
            return RedirectToAction("Payment");
        }

        // --- BƯỚC 2: CHỌN GIAO HÀNG & THANH TOÁN ---

        // GET: /Checkout/Payment
        [HttpGet]
        public IActionResult Payment()
        {
            // Kiểm tra đăng nhập và giỏ hàng
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = "/Checkout/Address" });
            }
            if (GetCart().Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            var model = GetCheckoutState();
            // Kiểm tra xem đã qua Bước 1 chưa (bằng cách check 1 trường bắt buộc)
            if (string.IsNullOrEmpty(model.TinhThanh))
            {
                return RedirectToAction("Address");
            }

            // Cập nhật giỏ hàng và tổng tiền vào model (để hiển thị)
            var cart = GetCart();
            model.CartItems = cart;
            model.TongTien = cart.Sum(item => item.ThanhTien);

            // TODO: Tính phí ship dựa trên model.TinhThanh
            // (Bạn có thể thêm logic tính phí ship ở đây)

            return View(model); // Tạo View "Views/Checkout/Payment.cshtml"
        }

        // POST: /Checkout/Payment
        [HttpPost]
        public IActionResult Payment(CheckoutViewModel model)
        {
            // Lấy trạng thái từ Session (không tin vào model post lên)
            var state = GetCheckoutState();

            // Chỉ cập nhật phương thức thanh toán
            state.PaymentMethod = model.PaymentMethod;

            // Lưu lại Session
            SaveCheckoutState(state);

            // Chuyển sang Bước 3
            return RedirectToAction("Confirm");
        }

        // --- BƯỚC 3: XÁC NHẬN ĐƠN HÀNG ---

        // GET: /Checkout/Confirm
        [HttpGet]
        public IActionResult Confirm()
        {
            // Kiểm tra đăng nhập và giỏ hàng
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = "/Checkout/Address" });
            }
            if (GetCart().Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            var model = GetCheckoutState();
            // Kiểm tra xem đã qua Bước 2 chưa
            if (string.IsNullOrEmpty(model.PaymentMethod))
            {
                return RedirectToAction("Payment");
            }

            // Lấy giỏ hàng và tổng tiền để hiển thị lần cuối
            var cart = GetCart();
            model.CartItems = cart;
            model.TongTien = cart.Sum(item => item.ThanhTien);
            // TODO: Thêm phí ship vào TongTien ở đây

            return View(model); // Tạo View "Views/Checkout/Confirm.cshtml"
        }

        // POST: /Checkout/Confirm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOrder() // Đổi tên để tránh trùng lặp
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = GetCart();
            var model = GetCheckoutState();
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Kiểm tra lần cuối
            if (cart.Count == 0 || string.IsNullOrEmpty(model.PaymentMethod) || string.IsNullOrEmpty(model.TinhThanh))
            {
                return RedirectToAction("Address");
            }

            // Gọi hàm xử lý COD (hoặc thanh toán online)
            if (model.PaymentMethod == "COD")
            {
                // Gọi hàm ProcessCOD với model từ Session
                return await ProcessCOD(model, userId, cart);
            }
            else
            {
                // Xử lý thanh toán online
                return RedirectToAction("ProcessOnlinePayment");
            }
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

        public IActionResult Index()
        {
            return RedirectToAction("Address");
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
            return View("OrderSuccess"); // Tạo View này
        }
    }
}