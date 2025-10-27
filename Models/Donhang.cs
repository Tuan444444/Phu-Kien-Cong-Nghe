using PhuKienCongNghe.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhuKienCongNghe.Models
{
    [Table("donhang")]
    public partial class Donhang
    {
        public Donhang()
        {
            Chitietdonhangs = new HashSet<Chitietdonhang>();
        }

        [Key]
        [Column("MaDonHang")]
        public int MaDonHang { get; set; }

        [Column("MaNguoiDung")]
        public int MaNguoiDung { get; set; }

        [Column("NgayDat", TypeName = "datetime2")]
        public DateTime? NgayDat { get; set; }

        [Column("TongTien")]
        public double TongTien { get; set; }

        [Column("HoTenNguoiNhan")]
        [StringLength(100)]
        public string? HoTenNguoiNhan { get; set; }

        [Column("SDTNguoiNhan")]
        [StringLength(15)]
        public string? SDTNguoiNhan { get; set; }

        [Column("TrangThai")]
        [StringLength(50)]
        public string? TrangThai { get; set; }

        [Column("DiaChiNhanHang")]
        public string? DiaChiNhanHang { get; set; } // nvarchar(MAX)

        [ForeignKey("MaNguoiDung")]
        [InverseProperty("Donhangs")]
        public virtual Nguoidung MaNguoiDungNavigation { get; set; } = null!;

        [InverseProperty("MaDonHangNavigation")]
        public virtual ICollection<Chitietdonhang> Chitietdonhangs { get; set; }
    }
}