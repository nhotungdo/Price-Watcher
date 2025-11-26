using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace PriceWatcher.Models;

public partial class PriceWatcherDbContext : DbContext
{
    public PriceWatcherDbContext()
    {
    }

    public PriceWatcherDbContext(DbContextOptions<PriceWatcherDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Platform> Platforms { get; set; }

    public virtual DbSet<PriceSnapshot> PriceSnapshots { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<SearchHistory> SearchHistories { get; set; }

    public virtual DbSet<SystemLog> SystemLogs { get; set; }

    public virtual DbSet<ProductMapping> ProductMappings { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<PriceAlert> PriceAlerts { get; set; }

    public virtual DbSet<CrawlJob> CrawlJobs { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    // New models for advanced features
    public virtual DbSet<StoreListing> StoreListings { get; set; }

    public virtual DbSet<Favorite> Favorites { get; set; }

    public virtual DbSet<DiscountCode> DiscountCodes { get; set; }

    public virtual DbSet<Store> Stores { get; set; }

    public virtual DbSet<ProductNews> ProductNews { get; set; }

    public virtual DbSet<AdminRole> AdminRoles { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<RolePermission> RolePermissions { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<AffiliateLink> AffiliateLinks { get; set; }

    public virtual DbSet<UserPreference> UserPreferences { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Data Source=NHOTUNG\\SQLEXPRESS;Database=PriceWatcherDB;User Id=sa;Password=123;TrustServerCertificate=true;Trusted_Connection=SSPI;Encrypt=false;");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Platform>(entity =>
        {
            entity.HasKey(e => e.PlatformId).HasName("PK__Platform__F559F6FAE2229549");

            entity.Property(e => e.ColorCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Domain)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PlatformName).HasMaxLength(50);
        });

        modelBuilder.Entity<PriceSnapshot>(entity =>
        {
            entity.HasKey(e => e.SnapshotId).HasName("PK__PriceSna__664F572B20732F6B");

            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.RecordedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Product).WithMany(p => p.PriceSnapshots)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__PriceSnap__Produ__412EB0B6");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Products__B40CC6CD901A2033");

            entity.Property(e => e.CurrentPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ExternalId)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.LastUpdated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ProductName).HasMaxLength(500);
            entity.Property(e => e.ShopName).HasMaxLength(200);

            entity.HasOne(d => d.Platform).WithMany(p => p.Products)
                .HasForeignKey(d => d.PlatformId)
                .HasConstraintName("FK__Products__Platfo__3D5E1FD2");
        });

        modelBuilder.Entity<SearchHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK__SearchHi__4D7B4ABDDBCE1A57");

            entity.Property(e => e.BestPriceFound).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.DetectedKeyword).HasMaxLength(200);
            entity.Property(e => e.SearchTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SearchType)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.User).WithMany(p => p.SearchHistories)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__SearchHis__UserI__44FF419A");
        });

        modelBuilder.Entity<SystemLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__SystemLo__5E5486481BF1B694");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Level)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C354C36F0");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D1053446CE8E92").IsUnique();

            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.GoogleId)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.LastLogin).HasColumnType("datetime");
            entity.Property(e => e.PasswordHash).HasColumnType("varbinary(64)");
            entity.Property(e => e.PasswordSalt).HasColumnType("varbinary(16)");
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.CartId);

            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasDefaultValueSql("(getutcdate())");

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasDefaultValueSql("(getutcdate())");

            entity.Property(e => e.ExpiresAt)
                .HasColumnType("datetime");

            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_Carts_UserId");
            entity.HasIndex(e => e.AnonymousId).HasDatabaseName("IX_Carts_AnonymousId");

            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.CartItemId);

            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.OriginalPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.AddedAt)
                .HasColumnType("datetime")
                .HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasDefaultValueSql("(getutcdate())");

            entity.HasIndex(e => new { e.CartId, e.ProductId, e.PlatformId })
                .HasDatabaseName("IX_CartItems_Cart_Product")
                .IsUnique(false);

            entity.HasOne(d => d.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(d => d.CartId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AdminRole>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasDefaultValueSql("(getutcdate())");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Resource).HasMaxLength(100);
            entity.Property(e => e.Action).HasMaxLength(50);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.PermissionId });
            entity.HasOne(e => e.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });
            entity.Property(e => e.AssignedAt)
                .HasColumnType("datetime")
                .HasDefaultValueSql("(getutcdate())");
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.AssignedBy)
                .WithMany()
                .HasForeignKey(e => e.AssignedByUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<UserPreference>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.LastUpdated).HasColumnType("datetime");
            entity.HasOne(e => e.User)
                .WithOne(u => u.UserPreference)
                .HasForeignKey<UserPreference>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Favorite>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TargetPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.LastViewedAt).HasColumnType("datetime");
            entity.HasOne(e => e.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product)
                .WithMany(p => p.Favorites)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Store>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Rating).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ResponseRate).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.JoinedDate).HasColumnType("datetime");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.LastUpdated).HasColumnType("datetime");
            entity.HasOne(e => e.Platform)
                .WithMany()
                .HasForeignKey(e => e.PlatformId);
        });

        modelBuilder.Entity<StoreListing>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.OriginalPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ShippingCost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.LastUpdated)
                .HasColumnType("datetime")
                .HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasDefaultValueSql("(getutcdate())");
            entity.HasOne(e => e.Product)
                .WithMany(p => p.StoreListings)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Platform)
                .WithMany()
                .HasForeignKey(e => e.PlatformId);
        });

        modelBuilder.Entity<DiscountCode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DiscountValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MinPurchase).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MaxDiscount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.LastVerifiedAt).HasColumnType("datetime");
            entity.HasOne(e => e.Platform)
                .WithMany()
                .HasForeignKey(e => e.PlatformId);
            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId);
            entity.HasOne(e => e.SubmittedBy)
                .WithMany()
                .HasForeignKey(e => e.SubmittedByUserId);
        });

        modelBuilder.Entity<ProductNews>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PublishedAt).HasColumnType("datetime");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasDefaultValueSql("(getutcdate())");
            entity.HasOne(e => e.Product)
                .WithMany(p => p.ProductNews)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AffiliateLink>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Revenue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CommissionRate).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.LastClickedAt).HasColumnType("datetime");
            entity.HasOne(e => e.Product)
                .WithMany(p => p.AffiliateLinks)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Platform)
                .WithMany()
                .HasForeignKey(e => e.PlatformId);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
