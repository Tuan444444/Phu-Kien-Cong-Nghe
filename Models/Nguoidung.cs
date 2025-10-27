using Microsoft.EntityFrameworkCore;
using PhuKienCongNghe.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhuKienCongNghe.Models
{
    [Table("nguoidung")]
    [Index(nameof(TenDangNhap), IsUnique = true)]
    [Index(nameof(Email), IsUnique = true)]
    public partial class Nguoidung
    {
        public Nguoidung()
        {
            Giohangs = new HashSet<Giohang>();
            Donhangs = new HashSet<Donhang>();
        }

        [Key]
        [Column("MaNguoiDung")]
        public int MaNguoiDung { get; set; }

        [Required]
        [Column("HoTen")]
        [StringLength(100)]
        public string HoTen { get; set; } = null!;

        [Required]
        [Column("TenDangNhap")]
        [StringLength(50)]
        public string TenDangNhap { get; set; } = null!;

        [Required]
        [Column("MatKhau")]
        [StringLength(255)]
        public string MatKhau { get; set; } = null!;

        [Required]
        [Column("Email")]
        [StringLength(100)]
        public string Email { get; set; } = null!;

        [Column("SoDienThoai")]
        [StringLength(15)]
        public string? SoDienThoai { get; set; }

        [Column("DiaChi")]
        public string? DiaChi { get; set; } // nvarchar(MAX)

        [Required]
        [Column("VaiTro")]
        [StringLength(10)]
        public string VaiTro { get; set; } = null!;

        [InverseProperty("MaNguoiDungNavigation")]
        public virtual ICollection<Giohang> Giohangs { get; set; }

        [InverseProperty("MaNguoiDungNavigation")]
        public virtual ICollection<Donhang> Donhangs { get; set; }
    }
}