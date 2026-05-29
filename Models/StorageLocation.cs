using System.Collections.Generic;

namespace MyAgenticProject.Models;

/// <summary>
/// 儲位主檔實體類別
/// </summary>
public class StorageLocation : BaseEntity
{
    /// <summary>
    /// 所屬資材室Id
    /// </summary>
    public int WarehouseId { get; set; }

    /// <summary>
    /// 所屬資材室導覽屬性
    /// </summary>
    public virtual Warehouse? Warehouse { get; set; }

    /// <summary>
    /// 儲位代號
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 儲位名稱
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 設以此儲位為預設存放位置的資產清單
    /// </summary>
    public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();

    /// <summary>
    /// 此儲位存放的庫存清單
    /// </summary>
    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
}
