namespace PhuKienCongNghe.Models
{
    public class CartItem
    {
        public int MaSanPham { get; set; }
        public string TenSanPham { get; set; }
        public string? HinhAnh { get; set; }
        public double Gia { get; set; }
        public int SoLuong { get; set; }

        // Tự động tính Thành Tiền (Rất tiện lợi)
        public double ThanhTien => Gia * SoLuong;
    }
}
