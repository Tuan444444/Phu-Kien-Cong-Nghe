using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhuKienCongNghe.ViewModels
{
    [Authorize]
    public class CheckoutViewModel
    {
        // Thông tin giỏ hàng để hiển thị tóm tắt
        public List<CartItemViewModel> CartItems { get; set; } = new List<CartItemViewModel>();
        public double TongTien { get; set; }

        // Phí vận chuyển và tạm tính
        public double PhiVanChuyen { get; set; }
        public double TamTinh { get; set; }

        // Thông tin người nhận (Bắt buộc)
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string? HoTenNguoiNhan { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? SoDienThoai { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        // Thông tin địa chỉ
        [Required(ErrorMessage = "Vui lòng chọn Tỉnh/Thành")]
        public string? TinhThanh { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Quận/Huyện")]
        public string? QuanHuyen { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Phường/Xã")]
        public string? PhuongXa { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ cụ thể")]
        public string? DiaChiCuThe { get; set; } // Số nhà, tên đường

        public string? GhiChu { get; set; }

        // Phương thức thanh toán
        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        public string? PaymentMethod { get; set; } // "COD" hoặc "Online"
    }
}
