using System.Text.Json.Serialization;
namespace PhuKienCongNghe.Models
{
    public class SanPhamNoiBat
    {
        public int MaSanPham { get; set; }
        public double GiaKhuyenMai { get; set; }

        // [JsonIgnore] rất quan trọng:
        // Báo cho hệ thống "Đừng lưu thuộc tính này vào file JSON"
        [JsonIgnore]
        public virtual Sanpham Sanpham { get; set; }
    }
}
