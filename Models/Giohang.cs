using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhuKienCongNghe.Models
{
    [Table("giohang")]
    public partial class Giohang
    {
        [Key]
        [Column("MaGioHang")]
        public int MaGioHang { get; set; }

        [Column("MaNguoiDung")]
        public int MaNguoiDung { get; set; }

        [Column("MaSanPham")]
        public int MaSanPham { get; set; }

        [Column("SoLuong")]
        public int SoLuong { get; set; }

        [ForeignKey("MaNguoiDung")]
        [InverseProperty("Giohangs")]
        public virtual Nguoidung MaNguoiDungNavigation { get; set; } = null!;

        [ForeignKey("MaSanPham")]
        [InverseProperty("Giohangs")]
        public virtual Sanpham MaSanPhamNavigation { get; set; } = null!;
    }
}