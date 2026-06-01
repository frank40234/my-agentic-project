---
trigger: model_decision
description: "C# / .NET 語言慣用規範。涵蓋命名、async/await、LINQ、EF Core 等最佳實踐。"
---

# C# / .NET 語言慣用規範

## 命名慣例

- **PascalCase**：類別、方法、屬性、事件、公開欄位
- **camelCase**：區域變數、方法參數
- **_camelCase**：私有欄位（底線前綴）
- **I 開頭**：介面名稱一律以 `I` 開頭（如 `IUserRepository`）
- **Async 後綴**：非同步方法名稱加 `Async`（如 `GetUserAsync`）
- **禁止匈牙利命名法**：不使用型別前綴（如 `strName`、`intCount`）

```csharp
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public async Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindAsync(userId, cancellationToken);
        return user;
    }
}
```

## async/await

- **所有 I/O 操作必須非同步**：資料庫查詢、HTTP 請求、檔案讀寫一律使用 async 版本
- **方法名稱加 Async 後綴**：回傳 `Task` 或 `Task<T>` 的方法必須加 `Async`
- **禁止 `.Result` 和 `.Wait()`**：這些會造成死鎖，一律使用 `await`
- **傳遞 CancellationToken**：所有非同步方法應接受並傳遞 `CancellationToken`
- **避免 async void**：僅限事件處理常式使用，其餘一律回傳 `Task`
- **使用 ConfigureAwait(false)**：在函式庫專案中使用，避免不必要的同步上下文切換

```csharp
// ✅ 正確
public async Task<List<Order>> GetOrdersAsync(CancellationToken ct = default)
{
    return await _dbContext.Orders
        .Where(o => o.IsActive)
        .ToListAsync(ct);
}

// ❌ 錯誤 - 會造成死鎖
public List<Order> GetOrders()
{
    return _dbContext.Orders
        .Where(o => o.IsActive)
        .ToListAsync().Result;
}
```

## Nullable Reference Types

- **啟用 NRT**：在 `.csproj` 中設定 `<Nullable>enable</Nullable>`
- **明確標記可為 null 的型別**：使用 `?` 運算子（如 `string?`、`User?`）
- **謹慎使用 `!` 運算子**：僅在確定不為 null 時使用，並加註解說明原因
- **使用 null 條件運算子**：`?.` 和 `??` 替代明確的 null 檢查
- **方法參數驗證**：對不可為 null 的參數使用 `ArgumentNullException.ThrowIfNull()`

```csharp
// 在 .csproj 中啟用
// <Nullable>enable</Nullable>

public async Task<UserDto?> GetUserAsync(string userId)
{
    ArgumentNullException.ThrowIfNull(userId);

    var user = await _repository.FindAsync(userId);
    return user?.ToDto();
}
```

## LINQ

- **優先使用方法語法**：`.Where().Select().OrderBy()` 而非查詢語法
- **避免在迴圈中執行 LINQ 查詢**：先具體化（`ToList()`/`ToArray()`）再迭代
- **合理使用延遲執行**：了解 `IEnumerable<T>` 與 `IQueryable<T>` 的差異
- **避免多次列舉**：對同一序列需多次存取時，先呼叫 `ToList()`
- **使用具體型別**：回傳 `List<T>` 或 `IReadOnlyList<T>` 而非 `IEnumerable<T>`（除非刻意延遲）

```csharp
// ✅ 正確 - 先具體化再使用
var activeUsers = await _dbContext.Users
    .Where(u => u.IsActive)
    .OrderBy(u => u.Name)
    .Select(u => new UserDto(u.Id, u.Name, u.Email))
    .ToListAsync(ct);

// ❌ 錯誤 - 迴圈中重複查詢
foreach (var orderId in orderIds)
{
    var order = _dbContext.Orders.FirstOrDefault(o => o.Id == orderId);
}
```

## EF Core

- **使用 Fluent API 設定**：不使用 Data Annotation，統一在 `IEntityTypeConfiguration<T>` 中設定
- **DbContext 生命週期為 Scoped**：透過 DI 容器管理，不手動 `new`
- **Migration 命名格式**：`YYYYMMDDHHMMSS_描述`（如 `20260601_AddUserTable`）
- **分離讀寫**：複雜查詢使用 `AsNoTracking()` 提升效能
- **避免懶載入**：明確使用 `Include()` / `ThenInclude()` 載入關聯資料
- **使用 ValueObject**：將 domain 值物件對應至 owned types

```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();
    }
}
```

## 依賴注入

- **使用 `builder.Services` 註冊**：統一在 `Program.cs` 或擴充方法中註冊
- **生命週期選擇**：Scoped 優先於 Transient，Singleton 僅用於無狀態或執行緒安全的服務
- **介面抽象**：每個服務透過介面註冊，方便測試替換
- **避免 Service Locator**：不直接使用 `IServiceProvider.GetService()`
- **使用 Options Pattern**：設定值透過 `IOptions<T>` / `IOptionsSnapshot<T>` 注入

```csharp
// 在擴充方法中組織註冊邏輯
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserService, UserService>();
        services.AddSingleton<ICacheService, RedisCacheService>();
        return services;
    }
}
```

## 例外處理

- **自定義例外繼承自 `Exception`**：建立領域專用的例外類別
- **使用 middleware 統一處理**：透過 `IExceptionHandler`（.NET 8+）或自訂 middleware
- **不吞掉例外**：`catch` 區塊必須記錄或重新拋出
- **使用 `when` 過濾**：`catch (Exception ex) when (ex is not OperationCanceledException)`
- **Result Pattern**：對預期的失敗情境使用 `Result<T>` 而非例外

```csharp
// 自定義領域例外
public class EntityNotFoundException : Exception
{
    public string EntityType { get; }
    public object EntityId { get; }

    public EntityNotFoundException(string entityType, object entityId)
        : base($"找不到 {entityType}（ID: {entityId}）")
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}
```

## 專案結構

- **`.sln` 放根目錄**：方案檔放在儲存庫根目錄
- **每個專案一個 `.csproj`**：依職責分層建立專案
- **遵循 Clean Architecture 分層**：

```
Solution.sln
├── src/
│   ├── Domain/              # 實體、值物件、領域事件
│   ├── Application/         # 用例、介面、DTO
│   ├── Infrastructure/      # 資料存取、外部服務實作
│   └── WebApi/              # 控制器、中介層、啟動設定
├── tests/
│   ├── UnitTests/
│   ├── IntegrationTests/
│   └── ArchitectureTests/
└── docs/
```