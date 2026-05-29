using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MyAgenticProject.Models;

namespace MyAgenticProject.Data;

/// <summary>
/// EAM 系統之資料庫上下文類別
/// </summary>
public class AppDbContext : DbContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// 初始化 AppDbContext 的新執行個體
    /// </summary>
    public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<MajorCategory> MajorCategories => Set<MajorCategory>();
    public DbSet<MinorCategory> MinorCategories => Set<MinorCategory>();
    public DbSet<ItemName> ItemNames => Set<ItemName>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StorageLocation> StorageLocations => Set<StorageLocation>();
    public DbSet<Inventory> Inventories => Set<Inventory>();

    /// <summary>
    /// 設定資料庫模型關聯與全域查詢篩選器 (IsActive == true)
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置大類
        modelBuilder.Entity<MajorCategory>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasQueryFilter(e => e.IsActive);
        });

        // 配置次類
        modelBuilder.Entity<MinorCategory>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasOne(d => d.MajorCategory)
                .WithMany(p => p.MinorCategories)
                .HasForeignKey(d => d.MajorCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => e.IsActive);
        });

        // 配置品名
        modelBuilder.Entity<ItemName>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasOne(d => d.MinorCategory)
                .WithMany(p => p.ItemNames)
                .HasForeignKey(d => d.MinorCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => e.IsActive);
        });

        // 配置單位
        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasQueryFilter(e => e.IsActive);
        });

        // 配置資產基本資訊
        modelBuilder.Entity<Asset>(entity =>
        {
            // 物料編碼必須唯一
            entity.HasIndex(e => e.MaterialCode).IsUnique();
            
            entity.HasOne(d => d.ItemName)
                .WithMany(p => p.Assets)
                .HasForeignKey(d => d.ItemNameId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Unit)
                .WithMany(p => p.Assets)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.DefaultStorageLocation)
                .WithMany(p => p.Assets)
                .HasForeignKey(d => d.DefaultStorageLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => e.IsActive);
        });

        // 配置資材室
        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasQueryFilter(e => e.IsActive);
        });

        // 配置儲位
        modelBuilder.Entity<StorageLocation>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasOne(d => d.Warehouse)
                .WithMany(p => p.StorageLocations)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => e.IsActive);
        });

        // 配置庫存表
        modelBuilder.Entity<Inventory>(entity =>
        {
            // 單一儲位下的單一資產只能有一筆庫存紀錄
            entity.HasIndex(e => new { e.AssetId, e.StorageLocationId }).IsUnique();

            entity.HasOne(d => d.Asset)
                .WithMany(p => p.Inventories)
                .HasForeignKey(d => d.AssetId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.StorageLocation)
                .WithMany(p => p.Inventories)
                .HasForeignKey(d => d.StorageLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => e.IsActive);
        });
    }

    /// <summary>
    /// 覆寫 SaveChanges，自動填寫建立與修改追蹤資訊
    /// </summary>
    public override int SaveChanges()
    {
        ApplyTrackingFields();
        return base.SaveChanges();
    }

    /// <summary>
    /// 覆寫 SaveChangesAsync，自動填寫建立與修改追蹤資訊
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTrackingFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// 自動寫入使用者追蹤欄位與時間戳記。
    /// </summary>
    private void ApplyTrackingFields()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        // 預設的使用者名稱，若無法自 HttpContext 取得 Claims 則使用 System
        string currentUser = "System";
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            // 嘗試取得工號與姓名
            var empNo = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var empName = httpContext.User.Identity.Name;
            if (!string.IsNullOrEmpty(empNo) && !string.IsNullOrEmpty(empName))
            {
                currentUser = $"{empNo}({empName})";
            }
            else if (!string.IsNullOrEmpty(empName))
            {
                currentUser = empName;
            }
        }

        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;
            var now = DateTime.Now;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedBy = currentUser;
                entity.CreatedTime = now;
                entity.IsActive = true; // 新增時預設啟用
            }

            entity.UpdatedBy = currentUser;
            entity.UpdatedTime = now;
        }
    }
}
