using PhuKienCongNghe.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhuKienCongNghe.Models
{
    [Table("danhmuc")]
    public partial class Danhmuc
    {
        public Danhmuc()
        {
            Sanphams = new HashSet<Sanpham>();
        }

        [Key]
        [Column("MaDanhMuc")]
        public int MaDanhMuc { get; set; }

        [Required]
        [Column("TenDanhMuc")]
        [StringLength(100)]
        public string TenDanhMuc { get; set; } = null!;

        [InverseProperty("MaDanhMucNavigation")]
        public virtual ICollection<Sanpham> Sanphams { get; set; }
    }
}