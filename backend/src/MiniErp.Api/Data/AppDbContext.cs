using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Domain;

namespace MiniErp.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.Property(u => u.Name).HasMaxLength(120);
            e.Property(u => u.Email).HasMaxLength(160);
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(u => u.Email).IsUnique();
        });

        b.Entity<Product>(e =>
        {
            e.Property(p => p.Sku).HasMaxLength(40);
            e.Property(p => p.Name).HasMaxLength(150);
            e.Property(p => p.Description).HasMaxLength(1000);
            e.Property(p => p.Category).HasMaxLength(60);
            e.Property(p => p.Unit).HasMaxLength(6);
            e.Property(p => p.SalePrice).HasPrecision(12, 2);
            e.Property(p => p.CostPrice).HasPrecision(14, 4);
            e.Property(p => p.Version).IsRowVersion();
            e.Ignore(p => p.IsBelowMinimum);
            e.HasIndex(p => p.Sku).IsUnique();
            e.HasIndex(p => p.Name);
        });

        b.Entity<StockMovement>(e =>
        {
            e.Property(m => m.Type).HasConversion<string>().HasMaxLength(10);
            e.Property(m => m.Origin).HasConversion<string>().HasMaxLength(20);
            e.Property(m => m.UnitCost).HasPrecision(14, 4);
            e.Property(m => m.Note).HasMaxLength(300);
            e.HasOne(m => m.Product).WithMany().HasForeignKey(m => m.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.User).WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<SalesOrder>().WithMany().HasForeignKey(m => m.SalesOrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(m => new { m.ProductId, m.CreatedAt });
            e.HasIndex(m => m.CreatedAt);
        });

        b.Entity<SalesOrder>(e =>
        {
            e.Property(o => o.Number).UseIdentityAlwaysColumn();
            e.Property(o => o.CustomerName).HasMaxLength(150);
            e.Property(o => o.CustomerDocument).HasMaxLength(20);
            e.Property(o => o.Notes).HasMaxLength(500);
            e.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(o => o.Total).HasPrecision(12, 2);
            e.HasOne(o => o.Seller).WithMany().HasForeignKey(o => o.SellerId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(o => o.Items).WithOne().HasForeignKey(i => i.SalesOrderId).OnDelete(DeleteBehavior.Cascade);
            e.Navigation(o => o.Items).HasField("_items");
            e.HasIndex(o => o.Number).IsUnique();
            e.HasIndex(o => new { o.Status, o.CreatedAt });
        });

        b.Entity<SalesOrderItem>(e =>
        {
            e.ToTable("sales_order_items");
            e.Property(i => i.ProductName).HasMaxLength(150);
            e.Property(i => i.Sku).HasMaxLength(40);
            e.Property(i => i.UnitPrice).HasPrecision(12, 2);
            e.Property(i => i.UnitCost).HasPrecision(14, 4);
            e.Ignore(i => i.Total);
            e.HasOne<Product>().WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        // Ids são gerados no domínio; sem isso o EF trata entidades novas em navegações como Modified
        foreach (var entity in b.Model.GetEntityTypes())
            entity.FindProperty("Id")?.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
    }
}
