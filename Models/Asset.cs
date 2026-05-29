using System.Collections.Generic;

namespace MyAgenticProject.Models;

/// <summary>
/// 資產基本資訊實體類別
/// </summary>
public class Asset : BaseEntity
{
    /// <summary>
    /// 所屬品名Id
    /// </summary>
    public int ItemNameId { get; set; }

    /// <summary>
    /// 所屬品名導覽屬性
    /// </summary>
    public virtual ItemName? ItemName { get; set; }

    /// <summary>
    /// 單位Id
    /// </summary>
    public int UnitId { get; set; }

    /// <summary>
    /// 單位導覽屬性
    /// </summary>
    public virtual Unit? Unit { get; set; }

    /// <summary>
    /// 預設存放儲位Id (可為空)
    /// </summary>
    public int? DefaultStorageLocationId { get; set; }

    /// <summary>
    /// 預設存放儲位導覽屬性
    /// </summary>
    public virtual StorageLocation? DefaultStorageLocation { get; set; }

    /// <summary>
    /// 型號
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// 廠牌
    /// </summary>
    public string Brand { get; set; } = string.Empty;

    /// <summary>
    /// 系統自動產生之物料編碼 (例如: CH-L1-001-0001)
    /// </summary>
    public string MaterialCode { get; set; } = string.Empty;

    /// <summary>
    /// 該資產對應的庫存異動紀錄
    /// </summary>
    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
}
