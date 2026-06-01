---
trigger: model_decision
description: "ERP 企業資源規劃系統領域規範。涵蓋財務精確度、單據狀態機、稽核追蹤、並發控制、跨模組事務一致性、多租戶隔離等企業級要求。"
---

## ERP 系統領域規範

本規範涵蓋 ERP（企業資源規劃）系統獨有的領域知識與約束。
與 `principles-large.md`（架構原則）、`lang-csharp.md`（語言規範）、`domain-web-api.md`（API 規範）互補，不重複。

---

### 1. 財務精確度

ERP 處理的是企業核心財務資料，任何精度遺失都可能導致帳務不平衡。

#### 金額計算
- **一律使用 `decimal` 型別**處理金額，禁止使用 `float` 或 `double`
- 金額欄位的資料庫型別為 `decimal(18,4)` 或依幣別需求調整精度
- 所有金額運算必須明確指定進位規則（`MidpointRounding.ToEven` 或依會計準則）
- 匯率計算結果的中間值保留至少 6 位小數，最終結果依幣別精度四捨五入
- 禁止在金額計算中使用字串轉換或浮點中繼

```csharp
// ✅ 正確
decimal taxAmount = Math.Round(subtotal * taxRate, 2, MidpointRounding.ToEven);

// ❌ 錯誤 — 浮點精度遺失
double taxAmount = (double)subtotal * (double)taxRate;
```

#### Value Object 封裝
- 金額建議封裝為 `Money` Value Object，攜帶幣別資訊
- Money 之間的運算必須檢查幣別一致性，不同幣別禁止直接加減

```csharp
public record Money(decimal Amount, string Currency)
{
    public Money
    {
        if (string.IsNullOrWhiteSpace(Currency))
            throw new DomainException("幣別不可為空");
    }

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new DomainException($"幣別不一致：{a.Currency} vs {b.Currency}");
        return new Money(a.Amount + b.Amount, a.Currency);
    }
}
```

---

### 2. 單據狀態機

ERP 的每一種單據（銷貨單、採購單、傳票、出貨單…）都有嚴格的狀態流轉規則。

#### 狀態設計原則
- 每種單據必須定義完整的**狀態流轉圖**，包含所有合法的狀態轉換
- 狀態轉換邏輯封裝在 **Domain Entity** 內部，禁止由 Service 或 Controller 直接修改 Status 欄位
- 不合法的狀態轉換必須拋出 `DomainException`（Fail Fast 原則）
- 每次狀態轉換必須記錄操作者、時間戳記與原因（稽核需求）

#### 典型狀態流轉

```
草稿 (Draft)
  ↓ 送審 Submit()
待審核 (PendingApproval)
  ↓ 核准 Approve()        ↘ 退回 Reject()
已核准 (Approved)          草稿 (Draft)
  ↓ 過帳 Post()
已過帳 (Posted)
  ↓ 作廢 Void()（需主管權限）
已作廢 (Voided)
```

#### 實作範例

```csharp
public class SalesOrder : AggregateRoot
{
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;

    /// <summary>送審：僅草稿狀態可送審。</summary>
    public void Submit(string submittedBy)
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException("ORD-001", $"僅草稿狀態可送審，目前狀態：{Status}");
        if (Items.Count == 0)
            throw new DomainException("ORD-002", "訂單明細不得為空");

        Status = OrderStatus.PendingApproval;
        AddDomainEvent(new OrderSubmittedEvent(Id, submittedBy));
    }

    /// <summary>核准：僅待審核狀態可核准。</summary>
    public void Approve(string approvedBy)
    {
        if (Status != OrderStatus.PendingApproval)
            throw new DomainException("ORD-003", $"僅待審核狀態可核准，目前狀態：{Status}");

        Status = OrderStatus.Approved;
        AddDomainEvent(new OrderApprovedEvent(Id, approvedBy));
    }

    /// <summary>過帳：僅已核准狀態可過帳，過帳後不可修改。</summary>
    public void Post(string postedBy)
    {
        if (Status != OrderStatus.Approved)
            throw new DomainException("ORD-004", $"僅已核准狀態可過帳，目前狀態：{Status}");

        Status = OrderStatus.Posted;
        PostedAt = DateTime.UtcNow;
        PostedBy = postedBy;
        AddDomainEvent(new OrderPostedEvent(Id, TotalAmount));
    }
}
```

#### 禁止事項
- ❌ 禁止從外部直接設定 `entity.Status = OrderStatus.Posted`
- ❌ 禁止跳過中間狀態（如從 Draft 直接到 Posted）
- ❌ 禁止已過帳的單據被修改內容（只能作廢後重開）

---

### 3. 稽核追蹤

ERP 的所有重要操作必須留下不可竄改的稽核紀錄。

#### 稽核欄位（每張表必備）

```csharp
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    string CreatedBy { get; set; }
    DateTime? UpdatedAt { get; set; }
    string? UpdatedBy { get; set; }
}
```

- 由 `DbContext.SaveChangesAsync()` 攔截器自動填入，禁止手動設定
- `CreatedAt` / `CreatedBy` 在新增後禁止修改

#### 稽核日誌（AuditLog）

以下操作必須寫入獨立的 AuditLog 資料表：
- 使用者登入/登出
- 單據的狀態轉換（建立、送審、核准、過帳、作廢）
- 主檔資料的新增、修改、刪除（客戶、供應商、品項、會計科目）
- 權限變更（角色指派、權限授予/撤銷）
- 系統設定變更

#### 不可篡改性

```sql
-- 在資料庫層級設定，任何程式碼都無法繞過
DENY UPDATE, DELETE ON dbo.AuditLog TO [AppUser];
```

- AuditLog 資料表在資料庫層級禁止 UPDATE 和 DELETE
- 應用程式碼中禁止對 AuditLog 執行任何修改或刪除操作
- 日誌內容必須包含：操作時間、操作者、操作類型、受影響的實體、變更前後的值

---

### 4. 樂觀並發控制

ERP 是多人同時操作的系統，必須防止靜默的資料覆蓋。

#### 預設策略：樂觀鎖

- 所有可被多人編輯的 Entity 必須包含 `RowVersion` 欄位
- 使用 EF Core 的 `[Timestamp]` 或 `IsRowVersion()` 設定
- 當偵測到衝突時，回傳明確的錯誤訊息告知使用者

```csharp
public class InventoryItem : IAuditable
{
    public int Id { get; set; }
    public string ProductId { get; set; }
    public int Quantity { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; }
}
```

#### 衝突處理

```csharp
try
{
    await _unitOfWork.CommitAsync();
}
catch (DbUpdateConcurrencyException)
{
    throw new BusinessRuleException("CONCURRENCY",
        "此資料已被其他使用者修改，請重新載入後再試。");
}
```

#### 何時改用悲觀鎖

| 場景 | 策略 |
|---|---|
| 一般單據編輯 | 樂觀鎖（預設） |
| 庫存即時扣減（高頻寫入） | 悲觀鎖（SELECT FOR UPDATE） |
| 自動編號產生（流水號） | 悲觀鎖或資料庫序列（Sequence） |
| 批次過帳（大量寫入） | 悲觀鎖 + 佇列序列化 |

---

### 5. 跨模組事務一致性

ERP 的核心操作往往涉及多個模組的資料變更，必須確保全部成功或全部回滾。

#### Unit of Work 強制要求

以下操作必須在同一個 Unit of Work（Transaction）中完成：

| 業務操作 | 涉及的模組 |
|---|---|
| 銷貨過帳 | 銷售（建出貨單）+ 庫存（扣減）+ 財務（建應收帳款）+ 稽核（寫日誌） |
| 採購驗收 | 採購（更新狀態）+ 庫存（增加）+ 財務（建應付帳款）+ 稽核 |
| 生產入庫 | 生產（完工）+ 庫存（成品入庫 + 原料扣減）+ 財務（成本結轉） |

```csharp
public async Task PostSalesOrderAsync(int orderId)
{
    var order = await _orderRepo.GetByIdAsync(orderId);
    order.Post(currentUser);

    await _shipmentRepo.CreateAsync(shipment);
    await _inventoryRepo.DeductAsync(items);
    await _receivableRepo.CreateAsync(receivable);
    await _auditRepo.LogAsync(auditEntry);

    // 一次性提交，全成功或全失敗
    await _unitOfWork.CommitAsync();
}
```

#### 跨模組通訊

- **同一 Bounded Context 內**：直接方法呼叫 + 同一 Transaction
- **跨 Bounded Context**：透過 Domain Event + 最終一致性（Eventual Consistency）
- 跨模組事件必須具備**冪等性**（重複處理不產生錯誤結果）
- 使用 **Outbox Pattern** 確保事件發布與資料庫寫入的原子性

---

### 6. 軟刪除

ERP 資料受法規約束（商業會計法、稅務法規），不可實際刪除。

#### 規則

- 所有交易類資料（訂單、傳票、出貨單、發票）**禁止 Hard Delete**
- 主檔資料（客戶、供應商、品項）使用軟刪除，刪除前檢查是否有未結交易
- 軟刪除必須記錄操作者與時間

#### 實作

```csharp
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}
```

- 使用 EF Core **Global Query Filter** 自動過濾已刪除資料
- 稽核查詢時使用 `IgnoreQueryFilters()` 暫時忽略過濾

```csharp
// DbContext 中設定
modelBuilder.Entity<Customer>()
    .HasQueryFilter(c => !c.IsDeleted);

// 稽核查詢時暫時忽略
var deletedCustomers = await _db.Customers
    .IgnoreQueryFilters()
    .Where(c => c.IsDeleted)
    .ToListAsync();
```

---

### 7. 多租戶資料隔離（選用）

若 ERP 需要服務多間公司（集團子公司、SaaS 模式），必須確保資料完全隔離。

#### 規則

- 每筆資料必須附帶 `TenantId`
- 所有查詢透過 Global Query Filter 自動過濾 `TenantId`
- `TenantId` 由後端從登入 Token 中取得，**禁止從前端傳入**
- 新增資料時由 `SaveChanges` 攔截器自動填入 `TenantId`
- 已寫入的 `TenantId` **禁止修改**

```csharp
public interface IMultiTenant
{
    string TenantId { get; set; }
}

// SaveChanges 攔截器
foreach (var entry in ChangeTracker.Entries<IMultiTenant>())
{
    if (entry.State == EntityState.Added)
        entry.Entity.TenantId = _tenantContext.CurrentTenantId;

    if (entry.State == EntityState.Modified)
        entry.Property(nameof(IMultiTenant.TenantId)).IsModified = false;
}
```

---

### 8. 傳票與借貸平衡

財務模組的核心約束：每一筆傳票的借方總額必須等於貸方總額。

#### 規則

- 傳票（Journal Entry）的 `DebitTotal` 必須等於 `CreditTotal`，否則禁止儲存
- 此驗證在 Domain Entity 內部執行（Fail Fast）
- 過帳操作必須在寫入前再次驗證平衡性

```csharp
public class JournalEntry : AggregateRoot
{
    private readonly List<JournalLine> _lines = new();

    public decimal DebitTotal => _lines.Where(l => l.IsDebit).Sum(l => l.Amount);
    public decimal CreditTotal => _lines.Where(l => !l.IsDebit).Sum(l => l.Amount);
    public bool IsBalanced => DebitTotal == CreditTotal;

    /// <summary>過帳前驗證借貸平衡。</summary>
    public void Post(string postedBy)
    {
        if (_lines.Count == 0)
            throw new DomainException("JE-001", "傳票明細不得為空");

        if (!IsBalanced)
            throw new DomainException("JE-002",
                $"借貸不平衡：借方 {DebitTotal} ≠ 貸方 {CreditTotal}");

        Status = JournalStatus.Posted;
        AddDomainEvent(new JournalPostedEvent(Id, DebitTotal));
    }
}
```

---

### 9. 編號規則引擎

ERP 的每種單據都需要系統自動產生唯一且有意義的單據編號。

#### 規則

- 編號格式可設定：前綴 + 日期 + 流水號（如 `SO-20260601-0001`）
- 流水號必須保證唯一性，即使在高併發環境下
- 使用資料庫序列（Sequence）或悲觀鎖產生流水號，**禁止在應用層用 Max+1**
- 編號一旦產生不可回收（即使單據被作廢，編號也不重用）
- 不同租戶（若啟用多租戶）的編號序列獨立

```csharp
public interface IDocumentNumberGenerator
{
    /// <summary>產生指定單據類型的下一個編號。</summary>
    Task<string> GenerateAsync(DocumentType type, string? tenantId = null);
}

// 實作使用資料庫序列，確保併發安全
// SELECT NEXT VALUE FOR dbo.Seq_SalesOrder;
```

---

### 10. 權限與資料範圍控制

ERP 的權限不只是「能不能存取某個功能」，還包括「能看到哪些資料」。

#### 功能權限（Function Permission）

- 每個 API 端點 / 操作對應一個權限代碼
- 使用 RBAC（角色為基礎的存取控制）
- 常見權限粒度：檢視（View）、新增（Create）、編輯（Edit）、刪除（Delete）、過帳（Post）、作廢（Void）

#### 資料範圍權限（Data Scope）

- 控制使用者能看到**哪些資料**（不只是能不能操作）
- 常見範圍：僅自己的資料、所屬部門、所屬公司、全部

| 範圍 | 說明 | 範例 |
|---|---|---|
| Self | 只能看到自己建立的單據 | 業務員只看自己的訂單 |
| Department | 能看到所屬部門的資料 | 部門主管看整個部門 |
| Company | 能看到所屬公司的資料 | 公司管理層 |
| All | 能看到所有資料 | 系統管理員、集團稽核 |

- 資料範圍過濾在 Repository 層透過 Query Filter 自動注入，**禁止在每個 Service 方法中手動判斷**

---

### 11. 期間控制

ERP 的財務模組需要控制會計期間的開關，防止對已關帳期間的資料進行異動。

#### 規則

- 系統維護「會計期間」設定，每個期間有開啟（Open）/ 關閉（Closed）狀態
- 所有會影響財務的操作（過帳、作廢、調整），必須檢查目標日期所屬的會計期間是否為開啟狀態
- 已關閉期間的資料禁止新增、修改或刪除
- 期間的開啟/關閉操作需要特殊權限（通常為財務主管）

```csharp
public class AccountingPeriodService
{
    /// <summary>驗證指定日期的會計期間是否開啟。</summary>
    public async Task EnsurePeriodOpenAsync(DateTime transactionDate)
    {
        var period = await _periodRepo.GetByDateAsync(transactionDate);

        if (period == null)
            throw new DomainException("AP-001",
                $"找不到日期 {transactionDate:yyyy-MM-dd} 對應的會計期間");

        if (period.Status == PeriodStatus.Closed)
            throw new DomainException("AP-002",
                $"會計期間 {period.Name} 已關閉，無法執行財務操作");
    }
}
```

---

### 12. ERP 效能特殊考量

#### 報表查詢最佳化
- 報表查詢可**繞過 Domain Layer**，直接使用 Dapper 或 Raw SQL
- 複雜報表考慮使用**物化視圖（Materialized View）** 或預先彙總表
- 大量資料匯出使用串流方式（Streaming），禁止一次載入全部到記憶體

#### 批次操作
- 大量資料異動（月結、年結、批次過帳）使用 **Bulk Insert / Update**
- 批次操作必須有進度回報機制（已處理 N / 總共 M 筆）
- 批次操作必須支援中斷續跑（記錄處理到哪一筆）

#### 主檔快取
- 變動頻率極低的主檔資料（幣別、會計科目、部門、稅率）快取於記憶體或 Redis
- 主檔異動時主動清除快取（Cache Invalidation）
- 快取未命中時自動回查資料庫並補入快取

---

### 速查表

| # | 規範 | 一句話摘要 | 關鍵字 |
|---|---|---|---|
| 1 | 財務精確度 | 金額一律 decimal，禁止 float | `decimal(18,4)` |
| 2 | 單據狀態機 | 狀態轉換封裝在 Entity 內部 | `Fail Fast` |
| 3 | 稽核追蹤 | 重要操作必留不可篡改紀錄 | `DENY UPDATE DELETE` |
| 4 | 樂觀並發控制 | RowVersion 防止靜默覆蓋 | `[Timestamp]` |
| 5 | 跨模組事務 | 同一 Transaction 全成功或全失敗 | `UnitOfWork` |
| 6 | 軟刪除 | 交易資料禁止 Hard Delete | `IsDeleted` |
| 7 | 多租戶隔離 | TenantId 自動過濾，禁止前端傳入 | `Global Query Filter` |
| 8 | 借貸平衡 | 傳票借方 = 貸方，否則禁止儲存 | `IsBalanced` |
| 9 | 編號規則 | 資料庫序列產生，禁止 Max+1 | `Sequence` |
| 10 | 資料範圍權限 | 不只控制功能，還控制能看到哪些資料 | `Data Scope` |
| 11 | 期間控制 | 已關帳期間禁止財務異動 | `PeriodStatus` |
| 12 | 效能考量 | 報表繞過 Domain、批次支援續跑 | `Dapper` / `Bulk` |