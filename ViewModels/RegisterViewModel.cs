using System.ComponentModel.DataAnnotations;
namespace PhuKienCongNghe.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Họ tên")]
        public required string HoTen { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Tên đăng nhập")]
        public required string TenDangNhap { get; set; }
         
        [Required(ErrorMessage = "Vui lòng nhập Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Mật khẩu")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public required string MatKhau { get; set; }

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [DataType(DataType.Password)]
        [Compare("MatKhau", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [Display(Name = "Xác nhận Mật khẩu")]
        public required string XacNhanMatKhau { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? SoDienThoai { get; set; }

        public string? DiaChi { get; set; }
    }
}