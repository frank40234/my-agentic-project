using System.Collections.Generic;

namespace MyAgenticProject.Models;

/// <summary>
/// 資產次類實體類別
/// </summary>
public class MinorCategory : BaseEntity
{
    /// <summary>
    /// 所屬大類Id
    /// </summary>
    public int MajorCategoryId { get; set; }

    /// <summary>
    /// 所屬大類導覽屬性
    /// </summary>
    public virtual MajorCategory? MajorCategory { get; set; }

    /// <summary>
    /// 次類代號 (例如: L1)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 次類名稱
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 次類底下的所有品名清單
    /// </summary>
    public virtual ICollection<ItemName> ItemNames { get; set; } = new List<ItemName>();
}
