using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhuKienCongNghe.Models
{
    [Table("chitietdonhang")]
    public partial class Chitietdonhang
    {
        [Key]
        [Column("MaChiTiet")]
        public int MaChiTiet { get; set; }

        [Column("MaDonHang")]
        public int MaDonHang { get; set; }

        [Column("MaSanPham")]
        public int MaSanPham { get; set; }

        [Column("SoLuong")]
        public int SoLuong { get; set; }

        [Column("DonGia")]
        public double DonGia { get; set; }

        [ForeignKey("MaDonHang")]
        [InverseProperty("Chitietdonhangs")]
        public virtual Donhang MaDonHangNavigation { get; set; } = null!;

        [ForeignKey("MaSanPham")]
        [InverseProperty("Chitietdonhangs")]
        public virtual Sanpham MaSanPhamNavigation { get; set; } = null!;
    }
}