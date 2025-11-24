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

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
