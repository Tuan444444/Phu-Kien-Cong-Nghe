using System.ComponentModel.DataAnnotations;

namespace PhuKienCongNghe.ViewModels
{
    public class ProfileViewModel
    {
        [Display(Name = "Tên đăng nhập")]
        public string TenDangNhap { get; set; } // Chỉ để hiển thị, không cho sửa

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và Tên")]
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } // Chỉ để hiển thị, không cho sửa

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string? SoDienThoai { get; set; }

        [Display(Name = "Địa chỉ")]
        public string? DiaChi { get; set; }
    }
}