---
trigger: model_decision
description: "大型專案開發原則。適用於企業級、多團隊、長生命週期的專案。完整套用 SOLID、DDD 與 Clean Architecture。"
---

# 大型專案開發原則

> 適用於企業級、多團隊協作、長生命週期的專案。
> 本文件為 **強制性規範**，所有產出程式碼必須遵守以下原則。

---

## 1. SOLID 原則

### 1.1 單一職責原則（SRP）

- 每個類別只擁有一個改變的理由，對應一個業務關注點。
- 當一個類別同時處理驗證、持久化、通知等邏輯時，必須拆分。
- **可接受的違反情境**：極簡的 CRUD DTO 或 Mapping Profile，若拆分反而增加不必要的間接層。

### 1.2 開放封閉原則（OCP）

- 類別對擴展開放、對修改封閉。透過介面、抽象類別或策略模式實現擴展。
- 新增行為時應新增實作類別，而非修改既有類別的 `if/switch` 分支。
- **可接受的違反情境**：原型驗證階段的臨時程式碼，但須標記 `// TODO: OCP - 提取策略` 並在下一個迭代重構。

### 1.3 里氏替換原則（LSP）

- 子類別必須能完全替換父類別，不改變程式的正確性。
- 禁止在子類別中拋出父類別未宣告的例外，或收窄前置條件。
- **可接受的違反情境**：裝飾器模式中故意增強行為（如加入快取層），但必須在文件中明確說明行為差異。

### 1.4 介面隔離原則（ISP）

- 不強迫類別實作不需要的方法。介面應細粒度，按角色拆分。
- 一個介面超過 5 個方法時，檢視是否需要拆分。
- **可接受的違反情境**：框架強制實作的介面（如 `IHostedService`），可保留空實作但加註 `// 框架要求，無業務邏輯`。

### 1.5 依賴反轉原則（DIP）

- 高層模組不依賴低層模組，兩者都依賴抽象。
- Domain Layer 與 Application Layer 禁止直接參考 Infrastructure 的具體類別。
- 所有外部依賴（資料庫、HTTP Client、檔案系統）透過介面注入。
- **可接受的違反情境**：`Program.cs` / Composition Root 中的直接參考，因為那是組裝依賴的唯一合法位置。

---

## 2. Clean Architecture / Hexagonal Architecture

架構分為四層，依賴方向嚴格由外向內。內層不得知道外層的存在。

```
Presentation → Application → Domain
                   ↑
             Infrastructure
```

### 2.1 Domain Layer（核心層）

- **位置**：`{BoundedContext}.Domain/`
- **內容**：
  - **Entity**：具有唯一識別（ID）的業務物件，封裝業務規則與不變條件。
  - **Value Object**：不可變物件，以值進行相等性比較（如 `Money`、`Email`、`Address`）。覆寫 `Equals()` 和 `GetHashCode()`。
  - **Domain Service**：跨多個 Entity 的業務邏輯，不適合放在單一 Entity 中時使用。
  - **Domain Event**：表達「領域中已發生的事實」，以過去式命名（如 `OrderPlacedEvent`）。
- **規則**：
  - 零外部依賴（不參考任何 NuGet 套件、不參考其他層）。
  - 不包含任何 I/O 操作（資料庫、網路、檔案系統）。
  - 所有業務規則驗證在此層完成。

### 2.2 Application Layer（應用層）

- **位置**：`{BoundedContext}.Application/`
- **內容**：
  - **Use Case / Command Handler**：處理寫入操作，協調 Domain 物件完成業務流程。
  - **Query Handler**：處理讀取操作，可直接使用最佳化查詢（不需經過 Domain 物件）。
  - **介面定義**：`IRepository<T>`、`IUnitOfWork`、`IEventPublisher` 等抽象介面定義在此層。
  - **DTO / Input Model**：接收外部輸入的資料結構，與 Domain Entity 明確分離。
- **規則**：
  - 依賴 Domain Layer，不依賴 Infrastructure 或 Presentation。
  - 每個 Use Case 一個 Handler 類別，避免 God Service。
  - 使用 MediatR 或類似中介者模式分派 Command/Query。

### 2.3 Infrastructure Layer（基礎設施層）

- **位置**：`{BoundedContext}.Infrastructure/`
- **內容**：
  - **Repository 實作**：實作 Application Layer 定義的 `IRepository<T>` 介面。
  - **外部 API Adapter**：封裝第三方服務呼叫（支付、郵件、訊息佇列）。
  - **持久化設定**：Entity Framework DbContext、Dapper 查詢、Redis 快取。
  - **事件發布實作**：實作 `IEventPublisher`，將 Domain Event 發布至訊息佇列或事件匯流排。
- **規則**：
  - 實作 Application Layer 的介面，不被其他層直接參考。
  - 每個外部依賴獨立一個 Adapter 類別，便於替換與測試。
  - 所有資料庫查詢使用參數化語法，禁止字串拼接。

### 2.4 Presentation Layer（表現層）

- **位置**：`{BoundedContext}.Api/` 或 `{BoundedContext}.Web/`
- **內容**：
  - **Controller / Endpoint**：接收 HTTP 請求，轉換為 Command/Query 並分派。Controller 不包含業務邏輯。
  - **DTO（Response Model）**：回傳給客戶端的資料結構，與 Domain Entity 嚴格分離。
  - **Middleware**：橫切關注點處理（認證、授權、例外攔截、請求日誌）。
  - **Validator**：輸入驗證（FluentValidation），在進入 Application Layer 前攔截無效請求。
- **規則**：
  - Controller 的方法體不超過 10 行，僅負責「接收 → 分派 → 回傳」。
  - 禁止在 Controller 中直接注入 Repository 或 DbContext。
  - API 回傳統一封裝格式（如 `ApiResponse<T>`），包含 `success`、`data`、`errors`。

---

## 3. DDD 戰術模式

### 3.1 Aggregate Root

- Aggregate 是一致性邊界，外部只能透過 Aggregate Root 存取內部 Entity。
- 每個 Aggregate Root 定義不變條件（invariants），在狀態變更時強制檢查。
- Repository 只為 Aggregate Root 建立，不為內部 Entity 建立獨立 Repository。
- Aggregate 之間透過 ID 參考，禁止直接持有其他 Aggregate 的物件參考。

```csharp
// 正確：透過 ID 參考
public class Order : AggregateRoot
{
    public CustomerId CustomerId { get; private set; }  // ID 參考
    private readonly List<OrderItem> _items = new();     // 內部 Entity
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    /// <summary>
    /// 新增訂單項目，並驗證不變條件（數量上限、庫存等）。
    /// </summary>
    public void AddItem(ProductId productId, int quantity, Money price)
    {
        // 不變條件檢查
        if (_items.Count >= 50)
            throw new DomainException("訂單項目不得超過 50 項");

        _items.Add(new OrderItem(productId, quantity, price));
        AddDomainEvent(new OrderItemAddedEvent(Id, productId));
    }
}
```

### 3.2 Value Object

- 不可變（所有屬性為 `init` 或 `readonly`）。
- 以所有屬性的組合進行相等性比較。
- 在建構子中執行自我驗證，無效值直接拒絕建立。

```csharp
public record Money(decimal Amount, string Currency)
{
    /// <summary>
    /// 驗證金額與幣別的合法性。
    /// </summary>
    public Money
    {
        if (Amount < 0) throw new DomainException("金額不可為負數");
        if (string.IsNullOrWhiteSpace(Currency)) throw new DomainException("幣別不可為空");
    }
}
```

### 3.3 Domain Event

- 命名使用過去式（`OrderPlaced`、`PaymentCompleted`）。
- 事件只攜帶必要的識別資訊（ID），不攜帶完整 Entity。
- 在 Aggregate Root 的方法中產生，透過 `AddDomainEvent()` 暫存。
- 由 Infrastructure Layer 在 `SaveChanges` 後發布。

### 3.4 Specification Pattern

- 將複雜查詢條件封裝為可組合的 Specification 物件。
- 支援 `And`、`Or`、`Not` 組合操作。
- Repository 接受 `ISpecification<T>` 參數，內部轉換為查詢表達式。

```csharp
/// <summary>
/// 查詢指定客戶在特定日期範圍內的訂單。
/// </summary>
public class CustomerOrdersInPeriodSpec : Specification<Order>
{
    public CustomerOrdersInPeriodSpec(CustomerId customerId, DateRange period)
    {
        Query.Where(o => o.CustomerId == customerId)
             .Where(o => o.CreatedAt >= period.Start && o.CreatedAt <= period.End)
             .OrderByDescending(o => o.CreatedAt);
    }
}
```

---

## 4. CQRS（Command Query Responsibility Segregation）

### 4.1 核心概念

- **Command（寫入）**：改變系統狀態的操作，不回傳業務資料（僅回傳成功/失敗）。
- **Query（讀取）**：查詢系統狀態的操作，不產生副作用。
- 讀寫使用不同的模型（Write Model 經過 Domain Layer，Read Model 可直接查詢最佳化視圖）。

### 4.2 實作規則

- Command Handler 必須經過完整的 Domain 驗證流程。
- Query Handler 可繞過 Domain Layer，直接使用 Dapper / Raw SQL / 讀取專用 DbContext 查詢。
- 讀取模型（Read Model / View Model）為扁平結構，針對 UI 需求最佳化。
- 禁止在 Query Handler 中修改任何狀態。

---

## 5. 領域事件發布/訂閱機制

### 5.1 事件流程

```
Domain Method → AddDomainEvent() → UnitOfWork.SaveChanges() → EventDispatcher → Handler(s)
```

### 5.2 規則

- **行程內事件**（同一請求內）：用於同一 Bounded Context 內的副作用（如更新快取、寫入審計日誌）。使用 MediatR `INotification` 或自建 Dispatcher。
- **跨 Context 事件**（非同步）：用於跨 Bounded Context 的溝通。透過訊息佇列（RabbitMQ / Azure Service Bus / Kafka）發布。
- 事件 Handler 必須具備冪等性（相同事件重複處理不產生錯誤結果）。
- 事件 Handler 失敗不應導致原始交易回滾（除非是行程內的強一致性需求）。
- 使用 Outbox Pattern 確保事件發布與資料庫寫入的原子性。

---

## 6. 錯誤處理

### 6.1 Result Pattern

- 業務邏輯層使用 `Result<T>` / `Result<T, TError>` 回傳成功或失敗，不使用 Exception 控制正常業務流程。
- Exception 僅用於「不可預期的系統錯誤」（如資料庫連線中斷、外部服務不可用）。

```csharp
/// <summary>
/// 處理結果封裝，避免使用 Exception 控制業務流程。
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(Error error) => new(false, default, error);
}
```

### 6.2 Domain Exception

- 自定義 `DomainException` 繼承自 `Exception`，僅用於不變條件被違反的嚴重錯誤。
- 所有 Domain Exception 必須包含機器可讀的錯誤代碼（`string ErrorCode`）。
- 在 Presentation Layer 的 Exception Middleware 中統一捕獲並轉換為 HTTP 回應。

### 6.3 錯誤分層

| 層級 | 錯誤處理方式 | 範例 |
|------|-------------|------|
| Domain | `Result<T>` 或 `DomainException` | 訂單金額為負、庫存不足 |
| Application | 接收 Domain 的 Result，轉換為統一回應 | 驗證失敗、權限不足 |
| Infrastructure | 捕獲外部異常，包裝為領域可理解的錯誤 | 資料庫逾時、API 不可用 |
| Presentation | 全域 Exception Middleware 兜底 | 回傳統一 JSON 格式 |

---

## 7. 設定管理

### 7.1 強型別 Options

- 每個設定區段對應一個 POCO 類別，使用 `IOptions<T>` / `IOptionsSnapshot<T>` 注入。
- Options 類別定義在 Application Layer，繫結在 Infrastructure 或 Presentation Layer。
- 禁止在程式碼中使用 `Configuration["Magic:String"]` 直接存取字串鍵值。

### 7.2 環境分離

- 使用 `appsettings.json` → `appsettings.{Environment}.json` 分層覆寫。
- 支援三個環境：`Development`、`Staging`、`Production`。
- 敏感設定（連線字串、API Key）在非開發環境中必須使用 Secret Manager、環境變數或 Key Vault，禁止寫入 appsettings 檔案。
- 每個 Options 類別實作 `IValidateOptions<T>`，在應用程式啟動時驗證設定完整性。

---

## 8. 日誌策略

### 8.1 結構化日誌

- 使用結構化日誌框架（Serilog / NLog），禁止字串插值組裝日誌訊息。
- 日誌訊息使用訊息模板（Message Template），屬性以具名參數傳入。

```csharp
// 正確：結構化日誌
_logger.LogInformation("訂單 {OrderId} 已建立，金額 {Amount}", orderId, amount);

// 錯誤：字串插值
_logger.LogInformation($"訂單 {orderId} 已建立，金額 {amount}");
```

### 8.2 Correlation ID 追蹤

- 每個 HTTP 請求在 Middleware 中生成或接收 `X-Correlation-Id` Header。
- Correlation ID 透過 `AsyncLocal<T>` 或 Logging Scope 傳遞至所有下游呼叫。
- 所有日誌條目自動附帶 `CorrelationId` 屬性。
- 呼叫外部服務時，將 Correlation ID 傳遞至下游（放入 HTTP Header）。

### 8.3 日誌層級使用規範

| 層級 | 使用場景 |
|------|---------|
| `Trace` | 方法進出、詳細變數狀態（僅開發環境啟用） |
| `Debug` | 診斷資訊，如快取命中/未命中 |
| `Information` | 業務事件（訂單建立、付款完成） |
| `Warning` | 可恢復的問題（重試、降級） |
| `Error` | 操作失敗但系統仍可運作 |
| `Critical` | 系統無法繼續運作（啟動失敗、資料毀損） |

---

## 9. 測試策略

### 9.1 測試金字塔

```
        ╱  E2E  ╲          少量、驗證關鍵流程
       ╱ 整合測試 ╲         中量、驗證跨層互動
      ╱  單元測試  ╲        大量、驗證業務邏輯
```

### 9.2 單元測試（Domain Layer）

- 測試對象：Entity、Value Object、Domain Service、Specification。
- 不使用任何 Mock（Domain Layer 無外部依賴）。
- 測試命名格式：`{Method}_When{Condition}_Should{Expected}`。
- 每個業務規則至少一個正向測試與一個反向測試。

### 9.3 整合測試（Application + Infrastructure）

- 測試對象：Command Handler、Query Handler、Repository 實作。
- 使用 Testcontainers 或 In-Memory Database 進行資料庫相關測試。
- Mock 外部 API Adapter（使用 WireMock 或自建 Fake）。
- 每個 Use Case 至少覆蓋：成功路徑、驗證失敗路徑、併發衝突路徑。

### 9.4 端對端測試（API）

- 測試對象：完整 HTTP 請求 → 回應流程。
- 使用 `WebApplicationFactory<T>` 啟動測試伺服器。
- 僅覆蓋關鍵業務流程（如：建立訂單 → 付款 → 出貨完整鏈路）。
- 測試資料使用 Builder Pattern 或 Factory 建立，禁止依賴外部種子資料。

### 9.5 測試覆蓋率目標

| 層級 | 最低覆蓋率 |
|------|-----------|
| Domain Layer | 90% |
| Application Layer | 80% |
| Infrastructure Layer | 70%（排除第三方套件包裝） |
| Presentation Layer | 關鍵端點 100%，其餘 60% |

---

## 10. 資料夾結構

按 Bounded Context 組織，每個 Context 內部再分層：

```
src/
├── Shared/                          # 跨 Context 共用（極少量）
│   ├── Shared.Domain/               # 共用 Value Object、基礎類別
│   ├── Shared.Application/          # 共用介面、行為管線
│   └── Shared.Infrastructure/       # 共用基礎設施（日誌、快取）
│
├── Ordering/                        # Bounded Context: 訂單
│   ├── Ordering.Domain/
│   │   ├── Entities/
│   │   ├── ValueObjects/
│   │   ├── Events/
│   │   ├── Services/
│   │   ├── Specifications/
│   │   └── Exceptions/
│   ├── Ordering.Application/
│   │   ├── Commands/
│   │   │   ├── PlaceOrder/
│   │   │   │   ├── PlaceOrderCommand.cs
│   │   │   │   ├── PlaceOrderHandler.cs
│   │   │   │   └── PlaceOrderValidator.cs
│   │   │   └── CancelOrder/
│   │   ├── Queries/
│   │   │   └── GetOrderById/
│   │   ├── Interfaces/              # IOrderRepository, IPaymentGateway
│   │   ├── DTOs/
│   │   └── Behaviors/               # 驗證、日誌等管線行為
│   ├── Ordering.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── OrderDbContext.cs
│   │   │   ├── Repositories/
│   │   │   ├── Configurations/      # EF Core Entity 設定
│   │   │   └── Migrations/
│   │   ├── ExternalServices/
│   │   └── EventPublishing/
│   └── Ordering.Api/
│       ├── Controllers/
│       ├── Middleware/
│       ├── Filters/
│       └── DTOs/                    # Request/Response Model
│
├── Catalog/                         # Bounded Context: 商品目錄
│   ├── Catalog.Domain/
│   ├── Catalog.Application/
│   ├── Catalog.Infrastructure/
│   └── Catalog.Api/
│
tests/
├── Ordering.Domain.Tests/
├── Ordering.Application.Tests/
├── Ordering.Infrastructure.Tests/
├── Ordering.Api.Tests/
└── Ordering.E2E.Tests/
```

### 資料夾結構規則

- 每個 Bounded Context 擁有獨立的解決方案資料夾，可獨立部署。
- `Shared/` 僅放置真正跨 Context 共用的基礎抽象，嚴禁業務邏輯外洩至此。
- Command / Query 使用「功能資料夾」（Feature Folder）組織，每個功能包含 Command + Handler + Validator。
- 禁止跨 Bounded Context 直接參考 Domain 或 Application 層，僅可透過事件或 API 通訊。