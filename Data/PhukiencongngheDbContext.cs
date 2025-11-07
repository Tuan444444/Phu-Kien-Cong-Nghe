using Microsoft.EntityFrameworkCore;
using PhuKienCongNghe.Models;

namespace PhuKienCongNghe.Data
{
    public partial class PhukiencongngheDbContext : DbContext
    {
        public PhukiencongngheDbContext(DbContextOptions<PhukiencongngheDbContext> options)
            : base(options)
        {
        }

        // Khai báo các bảng dưới dạng DbSet
        public virtual DbSet<Danhmuc> Danhmucs { get; set; } = null!;
        public virtual DbSet<Sanpham> Sanphams { get; set; } = null!;
        public virtual DbSet<Nguoidung> Nguoidungs { get; set; } = null!;
        public virtual DbSet<Giohang> Giohangs { get; set; } = null!;
        public virtual DbSet<Donhang> Donhangs { get; set; } = null!;
        public virtual DbSet<Chitietdonhang> Chitietdonhangs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Cấu hình các giá trị DEFAULT và ràng buộc
            // khớp với CSDL SQL Server của bạn

            modelBuilder.Entity<Sanpham>(entity =>
            {
                entity.Property(e => e.SoLuongTon).HasDefaultValue(0);

            });

            modelBuilder.Entity<Nguoidung>(entity =>
            {
                entity.Property(e => e.VaiTro).HasDefaultValue("user");
                // Ràng buộc CHECK đã được xử lý bởi [Index] và CSDL
            });

            modelBuilder.Entity<Giohang>(entity =>
            {
                entity.Property(e => e.SoLuong).HasDefaultValue(1);
            });

            modelBuilder.Entity<Donhang>(entity =>
            {
                entity.Property(e => e.NgayDat).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.TongTien).HasDefaultValue(0.0);
                entity.Property(e => e.TrangThai).HasDefaultValue("Chờ xử lý");
            });

            // Gọi phương thức partial nếu bạn cần mở rộng
            OnModelCreatingPartial(modelBuilder);
        }

        // Phương thức này cho phép bạn mở rộng OnModelCreating trong một file khác
        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}