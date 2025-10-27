using System.ComponentModel.DataAnnotations; // Cần cho Data Annotations

namespace PhuKienCongNghe.ViewModels
{
    public class CheckoutViewModel
    {
        // 3 thuộc tính này dùng để nhận dữ liệu từ Form POST
        [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận")]
        public string HoTenNguoiNhan { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string SDTNguoiNhan { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng")]
        public string DiaChiNhanHang { get; set; }

        // Thuộc tính này dùng để hiển thị tóm tắt giỏ hàng trên trang checkout
        // Nó sẽ được gán giá trị ở Controller (trong hàm GET Index)
        public CartViewModel Cart { get; set; }
    }
}