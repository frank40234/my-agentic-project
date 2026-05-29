using System.Collections.Generic;

namespace MyAgenticProject.Models;

/// <summary>
/// 單位主檔實體類別 (例如: 台、個、套)
/// </summary>
public class Unit : BaseEntity
{
    /// <summary>
    /// 單位代碼 (例如: PC, SET)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 單位名稱 (例如: 台, 個, 套)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 使用此單位的資產清單
    /// </summary>
    public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
}
