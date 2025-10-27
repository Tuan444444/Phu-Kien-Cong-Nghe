using PhuKienCongNghe.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhuKienCongNghe.Models
{
    [Table("sanpham")]
    public partial class Sanpham
    {
        public Sanpham()
        {
            Giohangs = new HashSet<Giohang>();
            Chitietdonhangs = new HashSet<Chitietdonhang>();
        }

        [Key]
        [Column("MaSanPham")]
        public int MaSanPham { get; set; }

        [Required]
        [Column("TenSanPham")]
        [StringLength(255)]
        public string TenSanPham { get; set; } = null!;

        [Column("HinhAnh")]
        [StringLength(255)]
        public string? HinhAnh { get; set; }

        [Column("Gia")]
        public double Gia { get; set; }

        [Column("MoTa")]
        public string? MoTa { get; set; } // nvarchar(MAX)

        [Column("SoLuongTon")]
        public int SoLuongTon { get; set; }

        [Column("MaDanhMuc")]
        public int MaDanhMuc { get; set; }

        [ForeignKey("MaDanhMuc")]
        [InverseProperty("Sanphams")]
        public virtual Danhmuc MaDanhMucNavigation { get; set; } = null!;

        [InverseProperty("MaSanPhamNavigation")]
        public virtual ICollection<Giohang> Giohangs { get; set; }

        [InverseProperty("MaSanPhamNavigation")]
        public virtual ICollection<Chitietdonhang> Chitietdonhangs { get; set; }
    }
}