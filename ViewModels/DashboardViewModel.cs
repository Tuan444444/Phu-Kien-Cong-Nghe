namespace PhuKienCongNghe.ViewModels
{
    public class DashboardViewModel
    {
        // HỘP 1: Doanh thu tháng này (SỬA)
        public double DoanhThuThangNay { get; set; }

        // HỘP 2: Đơn hàng mới (Giữ nguyên)
        public int DonHangMoi { get; set; }

        // HỘP 3: Tổng số Khách hàng (Giữ nguyên)
        public int TongKhachHang { get; set; }

        // HỘP 4: Sắp hết hàng (MỚI)
        public int SanPhamSapHetHang { get; set; }
    }
}
