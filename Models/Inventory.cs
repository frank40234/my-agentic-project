using System;

namespace MyAgenticProject.Models;

/// <summary>
/// 庫存實體類別
/// </summary>
public class Inventory : BaseEntity
{
    /// <summary>
    /// 資產Id
    /// </summary>
    public int AssetId { get; set; }

    /// <summary>
    /// 資產導覽屬性
    /// </summary>
    public virtual Asset? Asset { get; set; }

    /// <summary>
    /// 儲位Id
    /// </summary>
    public int StorageLocationId { get; set; }

    /// <summary>
    /// 儲位導覽屬性
    /// </summary>
    public virtual StorageLocation? StorageLocation { get; set; }

    /// <summary>
    /// 庫存數量
    /// </summary>
    public int Quantity { get; set; }
}
