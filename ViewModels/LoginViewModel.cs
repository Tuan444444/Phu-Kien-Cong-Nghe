using System.ComponentModel.DataAnnotations;
namespace PhuKienCongNghe.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Tên đăng nhập")]
        [Display(Name = "Tên đăng nhập")]
        public required string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Mật khẩu")]
        [DataType(DataType.Password)]
        public required string MatKhau { get; set; }
    }
}