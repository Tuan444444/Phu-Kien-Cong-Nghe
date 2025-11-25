using PhuKienCongNghe.Models;
using System.Collections.Generic;
using X.PagedList;
namespace PhuKienCongNghe.ViewModels
{
    public class ShopViewModel
    {
        // 1. Dùng để chứa danh sách SP Khuyến mãi (từ file JSON)
        public List<SanPhamNoiBat> FeaturedProducts { get; set; }

        // 2. Dùng để chứa TẤT CẢ sản phẩm (phân trang)
        public IPagedList<Sanpham> AllProducts { get; set; }

        // (Bạn có thể thêm các List<Danhmuc>... vào đây nếu cần)
        public List<Sanpham> BestSellers { get; set; }
    }
}
