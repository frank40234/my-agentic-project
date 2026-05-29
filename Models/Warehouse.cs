using System.Collections.Generic;

namespace MyAgenticProject.Models;

/// <summary>
/// 資材室主檔實體類別
/// </summary>
public class Warehouse : BaseEntity
{
    /// <summary>
    /// 資材室代碼
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 資材室名稱
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 資材室底下的所有儲位清單
    /// </summary>
    public virtual ICollection<StorageLocation> StorageLocations { get; set; } = new List<StorageLocation>();
}
