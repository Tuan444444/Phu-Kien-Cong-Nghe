using System.Collections.Generic; // Đảm bảo bạn có dòng này

namespace PhuKienCongNghe.ViewModels
{
    public class CartViewModel
    {
        // THÊM DÒNG NÀY VÀO:
        public List<CartItemViewModel> CartItems { get; set; } = new List<CartItemViewModel>();

        // Bạn cũng sẽ cần thuộc tính này cho trang giỏ hàng (theo code ở các bước trước)
        public double TongTien { get; set; }
    }
}
