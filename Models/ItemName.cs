using System.Collections.Generic;

namespace MyAgenticProject.Models;

/// <summary>
/// 品名主檔實體類別
/// </summary>
public class ItemName : BaseEntity
{
    /// <summary>
    /// 所屬次類Id
    /// </summary>
    public int MinorCategoryId { get; set; }

    /// <summary>
    /// 所屬次類導覽屬性
    /// </summary>
    public virtual MinorCategory? MinorCategory { get; set; }

    /// <summary>
    /// 品名代碼 (例如: 001)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 品名名稱
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 該品名對應的所有資產基本資訊清單
    /// </summary>
    public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
}
