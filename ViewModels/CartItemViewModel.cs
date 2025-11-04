namespace PhuKienCongNghe.ViewModels
{
    public class CartItemViewModel
    {

        public int MaSanPham { get; set; }
        public string TenSanPham { get; set; }
        public string HinhAnh { get; set; }
        public double Gia { get; set; }
        public int SoLuong { get; set; }
        public double ThanhTien => SoLuong * Gia;
    }
}