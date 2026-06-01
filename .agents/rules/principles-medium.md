---
trigger: model_decision
description: "中型專案開發原則。適用於多模組、需要長期維護的專案。平衡架構嚴謹度與開發速度。"
---

# 中型專案開發原則

> 適用於多模組（6–20 個主要功能）、需要長期維護、團隊協作的專案。核心精神：**在架構嚴謹度與開發速度之間取得平衡，只在有明確收益的地方引入抽象**。

---

## 1. 適度 SOLID

重點套用 **SRP（單一職責）** 和 **DIP（依賴反轉）**，不強求完美遵循 ISP 和 LSP。

### SRP（單一職責原則）— 嚴格套用

- 每個類別只負責一個明確的職責領域
- Controller 只負責 HTTP 處理（參數驗證、回應格式化），不包含業務邏輯
- Service 只負責業務邏輯編排，不直接操作資料庫
- Repository 只負責資料存取，不包含業務規則

### DIP（依賴反轉原則）— 嚴格套用

- 高層模組（Service）依賴抽象（Interface），不依賴具體實作（Repository）
- 所有外部依賴（資料庫、第三方 API、檔案系統）必須透過 interface 注入
- 這使得單元測試可以透過 mock 隔離外部依賴

### ISP / LSP — 務實套用

- 不刻意拆分 interface 到每個方法一個 interface（ISP 過度套用）
- 若一個 interface 有 5 個方法且所有實作都用到全部方法，不需要拆分
- LSP 在使用繼承時注意即可，優先使用組合避免繼承問題

```
# ✅ 務實的 interface 設計
class IUserRepository(ABC):
    def get_by_id(self, user_id: int) -> User: ...
    def get_by_email(self, email: str) -> User: ...
    def create(self, user: User) -> User: ...
    def update(self, user: User) -> User: ...
    def delete(self, user_id: int) -> None: ...

# ❌ 過度拆分（ISP 走火入魔）
class IUserReader(ABC): ...
class IUserWriter(ABC): ...
class IUserDeleter(ABC): ...
```

## 2. Repository Pattern

- 所有資料存取邏輯封裝在 Repository 類別中
- 每個 Entity / Aggregate Root 對應一個 Repository interface 和實作
- Repository 方法命名要表達意圖：`find_active_users()` 而非 `get_users_where_active_true()`
- 複雜查詢封裝在 Repository 中，Service 不直接組裝 SQL / ORM 查詢
- 允許建立跨 Entity 的查詢 Repository（如 `ReportRepository`），但不要濫用

```
# Repository 介面定義
class IOrderRepository(ABC):
    @abstractmethod
    def find_pending_by_customer(self, customer_id: int) -> list[Order]: ...

    @abstractmethod
    def find_overdue(self, threshold_date: datetime) -> list[Order]: ...

# Service 使用 Repository
class OrderService:
    def __init__(self, order_repo: IOrderRepository):
        self._order_repo = order_repo

    def get_customer_pending_orders(self, customer_id: int) -> list[OrderDTO]:
        orders = self._order_repo.find_pending_by_customer(customer_id)
        return [OrderDTO.from_entity(o) for o in orders]
```

## 3. Dependency Injection

- 使用框架內建的 DI 容器（ASP.NET Core `IServiceCollection`、Spring `@Autowired`、NestJS `@Injectable`）
- 所有 Service、Repository 透過 DI 容器註冊和解析
- **建構式注入**為唯一允許的注入方式，禁止屬性注入和方法注入
- DI 註冊集中管理在一個檔案（如 `startup.py`、`Program.cs`、`app.module.ts`）

### 生命週期管理

| 類型 | 生命週期 | 範例 |
|------|----------|------|
| Repository | Scoped（每次請求一個實例） | `UserRepository` |
| Service | Scoped | `OrderService` |
| 工具類別 | Singleton | `PasswordHasher`、`JwtTokenGenerator` |
| HttpClient | Singleton（使用 Factory） | `IHttpClientFactory` |

## 4. Service Layer

- 業務邏輯集中在 Service 類別中，Controller 和 Repository 不包含業務邏輯
- 每個功能模組一個 Service（如 `UserService`、`OrderService`）
- Service 方法對應一個完整的業務操作（Use Case）
- Service 之間可以互相呼叫，但避免循環依賴（A → B → A）
- 若出現循環依賴，抽取共用邏輯到新的 Service 或使用事件機制

```
class OrderService:
    """訂單相關業務邏輯。"""

    def __init__(
        self,
        order_repo: IOrderRepository,
        inventory_service: InventoryService,
        notification_service: NotificationService,
    ):
        self._order_repo = order_repo
        self._inventory = inventory_service
        self._notification = notification_service

    def place_order(self, request: PlaceOrderRequest) -> OrderDTO:
        """建立新訂單，扣減庫存，發送通知。"""
        # 驗證庫存
        self._inventory.check_availability(request.items)
        # 建立訂單
        order = Order.create(request)
        self._order_repo.save(order)
        # 扣減庫存
        self._inventory.deduct(request.items)
        # 通知
        self._notification.send_order_confirmation(order)
        return OrderDTO.from_entity(order)
```

## 5. DTO / ViewModel

- API 回傳**絕對不直接暴露 Entity**，一律透過 DTO 或 ViewModel 轉換
- 輸入（Request）和輸出（Response）使用不同的 DTO 類別
- DTO 只包含資料欄位和簡單的轉換方法，不包含業務邏輯
- 敏感欄位（密碼 hash、內部 ID、稽核欄位）不出現在 Response DTO 中

```
# ✅ 分離的 DTO
class CreateUserRequest:
    """建立使用者的請求資料。"""
    name: str
    email: str
    password: str

class UserResponse:
    """使用者的回應資料，排除敏感欄位。"""
    id: int
    name: str
    email: str
    created_at: datetime

# ❌ 直接回傳 Entity
@app.get("/users/{id}")
def get_user(id: int):
    return db.query(User).get(id)  # 會暴露 password_hash、internal_flags 等
```

## 6. 錯誤處理

### 全域例外處理中介軟體

- 建立全域例外處理器，統一捕獲未處理的例外並回傳標準錯誤格式
- 區分「業務例外」（回傳 4xx）和「系統例外」（回傳 5xx 並記錄日誌）

### 自定義例外類別

- 建立專案專用的例外基底類別
- 依據錯誤類型建立子類別，至少區分以下類型：

| 例外類別 | HTTP Status | 用途 |
|----------|-------------|------|
| `NotFoundException` | 404 | 資源不存在 |
| `ValidationException` | 400 | 輸入驗證失敗 |
| `ConflictException` | 409 | 資源衝突（重複建立等） |
| `UnauthorizedException` | 401 | 未認證 |
| `ForbiddenException` | 403 | 無權限 |
| `BusinessRuleException` | 422 | 違反業務規則 |

```
# 自定義例外階層
class AppException(Exception):
    """專案例外基底類別。"""
    def __init__(self, message: str, error_code: str = None):
        self.message = message
        self.error_code = error_code

class NotFoundException(AppException):
    """資源不存在。"""
    pass

# 全域例外處理
@app.exception_handler(AppException)
def handle_app_exception(request, exc: AppException):
    status_map = {
        NotFoundException: 404,
        ValidationException: 400,
        ConflictException: 409,
    }
    status = status_map.get(type(exc), 500)
    return JSONResponse(status_code=status, content={
        "error": exc.error_code or type(exc).__name__,
        "message": exc.message,
    })
```

## 7. 設定管理

- 使用 **Options Pattern**（或語言等效機制）將設定綁定到強型別類別
- 設定按功能分組：`DatabaseOptions`、`JwtOptions`、`SmtpOptions`
- 支援多環境設定：開發（Development）、測試（Staging）、正式（Production）
- 機密資訊使用 Secret Manager（開發）或環境變數/Vault（正式），不進版控

```
# 強型別設定類別
class DatabaseOptions:
    """資料庫連線設定。"""
    connection_string: str
    max_pool_size: int = 10
    command_timeout: int = 30

class JwtOptions:
    """JWT 認證設定。"""
    secret_key: str
    issuer: str
    audience: str
    expiration_minutes: int = 60

# 注入使用
class AuthService:
    def __init__(self, jwt_options: JwtOptions):
        self._jwt = jwt_options
```

## 8. 日誌

- 使用**結構化日誌**框架（Serilog / Winston / Python `structlog` / `logging` with JSON formatter）
- 日誌必須包含結構化欄位，不只是純文字訊息

### 日誌等級使用規範

| 等級 | 用途 | 範例 |
|------|------|------|
| `DEBUG` | 開發除錯用的詳細資訊 | SQL 查詢內容、變數值 |
| `INFO` | 業務操作的關鍵節點 | 使用者登入、訂單建立 |
| `WARNING` | 預期外但可恢復的狀況 | 重試成功、快取未命中 |
| `ERROR` | 操作失敗但系統可繼續運作 | 第三方 API 呼叫失敗 |
| `CRITICAL` | 系統無法繼續運作 | 資料庫連線中斷、設定檔缺失 |

### 結構化日誌範例

```
# ✅ 結構化日誌
logger.info("訂單建立成功",
    order_id=order.id,
    customer_id=customer.id,
    total_amount=order.total,
    item_count=len(order.items),
)

# ❌ 純文字日誌
logger.info(f"訂單 {order.id} 建立成功，金額 {order.total}")
```

### 必要的日誌埋點

- 每個 API 請求的進入和完成（使用中介軟體自動記錄）
- 所有外部服務呼叫（含耗時）
- 認證/授權失敗事件
- 業務操作的關鍵決策點

## 9. 測試策略

### 單元測試（核心邏輯）

- 所有 Service 類別的公開方法必須有單元測試
- 使用 mock 隔離外部依賴（Repository、外部 API Client）
- 測試案例涵蓋：正常路徑、邊界條件、錯誤情境
- 命名格式：`test_{方法名}_{情境}_{預期結果}`

```
# 測試命名範例
def test_place_order_insufficient_stock_raises_business_error():
    ...

def test_place_order_valid_request_returns_order_dto():
    ...
```

### 整合測試（API 端點）

- 每個 API 端點至少一個 happy path 整合測試
- 使用測試資料庫（SQLite in-memory 或 TestContainers）
- 測試 HTTP status code、回應格式、關鍵欄位值
- 測試認證/授權：未認證回傳 401、無權限回傳 403

### 不需要的測試

- 不需要 UI / E2E 測試（除非專案需求明確要求）
- 不需要測試框架自動產生的 CRUD 方法
- 不需要測試純粹的 DTO 轉換（除非包含複雜邏輯）

## 10. 資料夾結構

按功能模組分層，每個模組包含完整的分層結構：

```
project/
├── src/
│   ├── main.py                    # 進入點、DI 註冊
│   ├── config/
│   │   ├── settings.py            # 設定類別定義
│   │   └── database.py            # 資料庫初始化
│   ├── common/
│   │   ├── exceptions.py          # 自定義例外類別
│   │   ├── middleware.py          # 全域中介軟體
│   │   └── utils.py               # 共用工具函數
│   ├── users/
│   │   ├── controller.py          # HTTP 端點
│   │   ├── service.py             # 業務邏輯
│   │   ├── repository.py          # 資料存取
│   │   ├── models.py              # Entity / ORM 模型
│   │   ├── schemas.py             # Request / Response DTO
│   │   └── tests/
│   │       ├── test_service.py    # 單元測試
│   │       └── test_api.py        # 整合測試
│   ├── orders/
│   │   ├── controller.py
│   │   ├── service.py
│   │   ├── repository.py
│   │   ├── models.py
│   │   ├── schemas.py
│   │   └── tests/
│   │       ├── test_service.py
│   │       └── test_api.py
│   └── ...
├── migrations/                    # 資料庫遷移檔案
├── .env
├── .env.example                   # 環境變數範例（不含機密值）
└── README.md
```

### 結構原則

- 按**功能模組**（users、orders）分資料夾，而非按**技術層**（controllers、services）
- 每個模組的測試放在模組內的 `tests/` 資料夾
- `common/` 存放跨模組共用的程式碼
- `config/` 存放設定相關程式碼
- 新增模組時複製現有模組結構，確保一致性

---

## 何時該升級到大型專案架構？

當出現以下任一跡象時，考慮引入大型專案的架構實踐：

- 團隊超過 8 人，需要獨立部署的子系統
- 需要事件驅動架構或非同步訊息佇列
- 單一資料庫成為效能瓶頸，需要讀寫分離或分庫
- 需要導入 CQRS 或 Event Sourcing 模式
- 部署頻率差異大，不同模組需要獨立發版