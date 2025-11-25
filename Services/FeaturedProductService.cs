using PhuKienCongNghe.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
namespace PhuKienCongNghe.Services
{
    public class FeaturedProductService
    {
        private readonly string _filePath;
        private readonly IWebHostEnvironment _env;

        public FeaturedProductService(IWebHostEnvironment env)
        {
            _env = env;
            // Tìm đường dẫn tuyệt đối đến file
            _filePath = Path.Combine(_env.WebRootPath, "data", "khuyenmai.json");
        }

        // HÀM 1: Đọc tất cả từ file
        public List<SanPhamNoiBat> GetAll()
        {
            if (!File.Exists(_filePath))
            {
                return new List<SanPhamNoiBat>();
            }

            var json = File.ReadAllText(_filePath);

            if (string.IsNullOrEmpty(json) || json == "[]")
            {
                return new List<SanPhamNoiBat>();
            }

            return JsonSerializer.Deserialize<List<SanPhamNoiBat>>(json);
        }

        // HÀM 2: Ghi đè (Lưu) toàn bộ file
        private void SaveAll(List<SanPhamNoiBat> products)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(products, options);
            File.WriteAllText(_filePath, json);
        }

        // HÀM 3: Thêm 1 sản phẩm
        public void Add(SanPhamNoiBat product)
        {
            var products = GetAll();
            products.RemoveAll(p => p.MaSanPham == product.MaSanPham);
            products.Add(product);
            SaveAll(products);
        }

        // HÀM 4: Xóa 1 sản phẩm
        public void Delete(int maSanPham)
        {
            var products = GetAll();
            products.RemoveAll(p => p.MaSanPham == maSanPham);
            SaveAll(products);
        }
        public void Update(int maSanPham, double newPrice)
        {
            var products = GetAll();
            var product = products.FirstOrDefault(p => p.MaSanPham == maSanPham);

            if (product != null)
            {
                product.GiaKhuyenMai = newPrice;
                SaveAll(products); // Lưu lại thay đổi
            }
        }
    }
}
