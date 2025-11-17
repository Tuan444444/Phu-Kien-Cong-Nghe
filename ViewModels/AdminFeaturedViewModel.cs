using Microsoft.AspNetCore.Mvc.Rendering;
using PhuKienCongNghe.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace PhuKienCongNghe.ViewModels
{
    public class AdminFeaturedViewModel
    {
        public List<SanPhamNoiBat> FeaturedProducts { get; set; }
        public SelectList ProductsToFeature { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn một sản phẩm")]
        public int NewFeaturedProductId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá")]
        [Range(1, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        public double NewFeaturedPrice { get; set; }
    }
}
