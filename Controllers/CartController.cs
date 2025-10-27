using Microsoft.AspNetCore.Mvc;
using PhuKienCongNghe.Data;
using PhuKienCongNghe.Extensions; // Namespace của SessionExtensions
using PhuKienCongNghe.ViewModels;

namespace PhuKienCongNghe.Controllers
{
    public class CartController : Controller
    {
        private readonly PhukiencongngheDbContext _context;
        private const string CartSessionKey = "Cart";

        public CartController(PhukiencongngheDbContext context)
        {
            _context = context;
        }

        // Lấy giỏ hàng từ Session
        private List<CartItemViewModel> GetCart()
        {
            var cart = HttpContext.Session.Get<List<CartItemViewModel>>(CartSessionKey);
            if (cart == null)
            {
                cart = new List<CartItemViewModel>();
            }
            return cart;
        }

        // Lưu giỏ hàng vào Session
        private void SaveCart(List<CartItemViewModel> cart)
        {
            HttpContext.Session.Set(CartSessionKey, cart);
        }

        // Trang hiển thị giỏ hàng
        public IActionResult Index()
        {
            var cart = GetCart();
            var viewModel = new CartViewModel
            {
                CartItems = cart,
                TongTien = cart.Sum(item => item.ThanhTien)
            };
            return View(viewModel); // Cần tạo CartViewModel
        }

        // Thêm vào giỏ hàng (thường được gọi từ trang Details hoặc Index)
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _context.Sanphams.FindAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            var cart = GetCart();
            var cartItem = cart.FirstOrDefault(item => item.MaSanPham == productId);

            if (cartItem == null)
            {
                // Thêm mới
                cart.Add(new CartItemViewModel
                {
                    MaSanPham = product.MaSanPham,
                    TenSanPham = product.TenSanPham,
                    DonGia = product.Gia,
                    HinhAnh = product.HinhAnh ?? "", // Xử lý nếu HinhAnh là null
                    SoLuong = quantity
                });
            }
            else
            {
                // Cập nhật số lượng
                cartItem.SoLuong += quantity;
            }

            SaveCart(cart);
            return RedirectToAction("Index");
        }

        // Xóa khỏi giỏ hàng
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = GetCart();
            var cartItem = cart.FirstOrDefault(item => item.MaSanPham == productId);

            if (cartItem != null)
            {
                cart.Remove(cartItem);
                SaveCart(cart);
            }
            return RedirectToAction("Index");
        }

        // Cập nhật giỏ hàng (VD: thay đổi số lượng)
        [HttpPost]
        public IActionResult UpdateCart(int productId, int quantity)
        {
            if (quantity <= 0)
            {
                return RemoveFromCart(productId);
            }

            var cart = GetCart();
            var cartItem = cart.FirstOrDefault(item => item.MaSanPham == productId);

            if (cartItem != null)
            {
                cartItem.SoLuong = quantity;
                SaveCart(cart);
            }
            return RedirectToAction("Index");
        }
    }
}