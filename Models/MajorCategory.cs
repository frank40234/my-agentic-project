using System.Collections.Generic;

namespace MyAgenticProject.Models;

/// <summary>
/// 資產大類實體類別
/// </summary>
public class MajorCategory : BaseEntity
{
    /// <summary>
    /// 大類代號 (例如: CH)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 大類名稱
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 大類底下的所有次類清單
    /// </summary>
    public virtual ICollection<MinorCategory> MinorCategories { get; set; } = new List<MinorCategory>();
}
