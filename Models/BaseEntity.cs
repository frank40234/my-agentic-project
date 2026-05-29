using System;

namespace MyAgenticProject.Models;

/// <summary>
/// 通用實體基底類別，包含建立與修改人、時間以及邏輯刪除欄位。
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// 實體流水主鍵
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 建立者工號與姓名
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// 建立時間
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 修改者工號與姓名
    /// </summary>
    public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>
    /// 修改時間
    /// </summary>
    public DateTime UpdatedTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 啟用狀態 (false 代表停用/邏輯刪除)
    /// </summary>
    public bool IsActive { get; set; } = true;
}
