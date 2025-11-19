namespace PhuKienCongNghe.ViewModels
{
    public class TopProductViewModel
    {
        public string TenSanPham { get; set; }
        public int SoLuongDaBan { get; set; }

        // Tỷ lệ % (ví dụ: 80) để vẽ thanh progress-bar
        public int TyLePhanTram { get; set; }
    }
}
