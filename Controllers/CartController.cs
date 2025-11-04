using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Cần để dùng DbContext
using PhuKienCongNghe.Data;
using PhuKienCongNghe.Extensions; // Dùng file SessionExtensions.cs của bạn
using PhuKienCongNghe.Models;      // Dùng Models của bạn (Sanpham)
using PhuKienCongNghe.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PhuKienCongNghe.Controllers
{
    public class CartController : Controller
    {
        // Biến DbContext (Nhớ thay tên "PhuKienCongNgheContext" nếu bạn đặt khác)
        private readonly PhukiencongngheDbContext _context;

        // Tên của key lưu giỏ hàng trong Session
        public const string CARTKEY = "cart";

        // Constructor để "tiêm" DbContext vào
        public CartController(PhukiencongngheDbContext context)
        {
            _context = context;
        }

        // === CÁC HÀM NỘI BỘ (PRIVATE) ===

        // Hàm nội bộ để LẤY giỏ hàng từ Session
        private List<CartItemViewModel> GetCartItems()
        {
            // Dùng hàm "Get" từ file SessionExtensions.cs của bạn
            var cart = HttpContext.Session.Get<List<CartItemViewModel>>(CARTKEY);
            if (cart == null)
            {
                cart = new List<CartItemViewModel>(); // Nếu chưa có giỏ hàng, tạo 1 list rỗng
            }
            return cart;
        }

        // Hàm nội bộ để LƯU giỏ hàng vào Session
        private void SaveCartSession(List<CartItemViewModel> cart)
        {
            // Dùng hàm "Set" từ file SessionExtensions.cs của bạn
            HttpContext.Session.Set(CARTKEY, cart);
            // Tính tổng SỐ LƯỢNG (Quantity) của tất cả items
            int totalQuantity = cart.Sum(item => item.SoLuong);
            // Lưu tổng số lượng vào một Session key MỚI
            HttpContext.Session.SetInt32("CartCount", totalQuantity);
        }

        // === KẾT THÚC HÀM NỘI BỘ == 
      //  [HttpPost]
        public async Task<IActionResult> AddToCart(int id, int soLuong = 1)
        {
            // Tìm sản phẩm trong database
            var sanpham = await _context.Sanphams.FindAsync(id);
            if (sanpham == null)
            {
                return NotFound("Sản phẩm không tồn tại"); // Xử lý lỗi
            }

            // 1. Lấy giỏ hàng hiện tại từ Session
            var cart = GetCartItems();

            // 2. Kiểm tra xem sản phẩm đã có trong giỏ chưa
            var cartItem = cart.Find(p => p.MaSanPham == id);

            if (cartItem != null)
            {
                // Nếu đã có (khách mua thêm), chỉ tăng số lượng
                cartItem.SoLuong += soLuong;
            }
            else
            {
                // Nếu chưa có, tạo mới 1 CartItem từ Sanpham
                cartItem = new CartItemViewModel
                {
                    MaSanPham = id,
                    TenSanPham = sanpham.TenSanPham,
                    HinhAnh = sanpham.HinhAnh,
                    Gia = sanpham.Gia,
                    SoLuong = soLuong
                };
                cart.Add(cartItem); // Thêm vào giỏ hàng
            }

            // 3. Lưu lại giỏ hàng vào Session
            SaveCartSession(cart);
            TempData["ToastMessage"] = "Đã thêm sản phẩm vào giỏ hàng!";

            // 4. (Quan trọng) Quay lại đúng trang mà người dùng vừa bấm
            string referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
            {
                return Redirect(referer); // Quay lại trang trước đó
            }

            // Nếu không lấy được trang trước, về trang chủ
            return RedirectToAction("Index", "Home");
        }
        // POST: /Cart/UpdateCart
        [HttpPost] // Dùng [HttpPost] vì nó thay đổi dữ liệu
       
        public IActionResult UpdateCart(int id, int soLuong) // Action cũ của bạn
        {
            // Lấy giỏ hàng từ session
            var cart = GetCartItems();
            var cartItem = cart.Find(p => p.MaSanPham == id);

            if (cartItem != null)
            {
                cartItem.SoLuong = (soLuong > 0) ? soLuong : 1;
                SaveCartSession(cart);
            }
            return RedirectToAction("Index"); // Action này tải lại toàn bộ trang
        }


        //
        // --- THÊM ACTION MỚI NÀY VÀO ---
        //
        // Action này được thiết kế riêng cho JavaScript (AJAX)
        [HttpPost] // Chỉ chấp nhận yêu cầu POST
        public IActionResult UpdateCartAjax(int id, int soLuong)
        {
            var cart = GetCartItems();
            var cartItem = cart.Find(p => p.MaSanPham == id);

            if (cartItem != null)
            {
                // Cập nhật số lượng
                cartItem.SoLuong = (soLuong > 0) ? soLuong : 1;
                // Lưu lại session
                SaveCartSession(cart);

                // Tính toán tổng tiền mới
                double itemTotal = cartItem.ThanhTien;
                double cartTotal = cart.Sum(item => item.ThanhTien);
                int totalQuantity = cart.Sum(item => item.SoLuong);
                totalQuantity = totalQuantity;

                // Trả về kết quả dạng JSON
                return Json(new
                {
                    success = true,
                    itemTotal = itemTotal.ToString("N0") + " đ", // Định dạng tiền
                    cartTotal = cartTotal.ToString("N0") + " đ" // Định dạng tiền
                });
            }

            // Nếu thất bại
            return Json(new { success = false });
        }
        //
        // THÊM HÀM NÀY VÀO (2)
        //
        // GET: /Cart/RemoveFromCart/5
        public IActionResult RemoveFromCart(int id)
        {
            var cart = GetCartItems();
            var cartItem = cart.Find(p => p.MaSanPham == id);

            if (cartItem != null)
            {
                cart.Remove(cartItem); // Xóa khỏi danh sách
                SaveCartSession(cart); // Lưu lại
            }

            return RedirectToAction("Index");
        }

        // Action này để hiển thị trang Giỏ hàng (sẽ làm ở bước sau)
        public IActionResult Index()
        {
            var cart = GetCartItems();
            // Chúng ta sẽ tạo View cho cái này ở bước tiếp theo
            return View(cart);
        }
    }
}