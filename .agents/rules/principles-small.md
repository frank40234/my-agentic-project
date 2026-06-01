---
name: principles-small
description: "小型專案開發原則。適用於功能單純、模組數量少的專案。強調簡單、快速交付。"
alwaysApply: false
---

# 小型專案開發原則

> 適用於功能單純、模組數量少（≤5 個主要功能）的專案。核心精神：**能用簡單方案解決的，絕不引入複雜架構**。

---

## 1. KISS 原則（Keep It Simple, Stupid）

- 選擇最直觀的實作方式，不追求「優雅」或「彈性」
- 一個函數只做一件事，但不需要為了 SRP 拆出一堆只用一次的類別
- 若兩種寫法功能相同，選擇程式碼行數較少、認知負擔較低的那一種
- 避免使用設計模式僅僅因為「這是最佳實踐」——必須有具體的問題才引入對應的解法

```
# ✅ 簡單直接
def get_user(user_id):
    return db.query(User).filter(User.id == user_id).first()

# ❌ 過度設計
class UserRepositoryInterface(ABC): ...
class UserRepository(UserRepositoryInterface): ...
class UserService:
    def __init__(self, repo: UserRepositoryInterface): ...
```

## 2. YAGNI 原則（You Aren't Gonna Need It）

- 只實作當前需求明確要求的功能，不預留「未來可能用到」的擴充點
- 不提前建立 interface 來「方便日後替換實作」——等真正需要替換時再重構
- 不建立 factory、builder、strategy 等模式，除非當下就有多種實作需要切換
- 設定項只加目前會用到的，不預設一堆預設值為空的欄位

## 3. 最少抽象

- **禁止**建立只有一個實作的 interface 或 abstract class
- **禁止**建立 factory class 來包裝一個 `new` 呼叫
- **禁止**建立只做轉發的 wrapper class
- 泛型只在確實需要處理多種型別時使用，不為了「型別安全」而過度泛型化
- 繼承層數不超過 1 層（父類別 → 子類別），優先使用組合或直接實作

## 4. 直接呼叫優於間接呼叫

- **不需要 Repository Pattern**：直接在業務邏輯中存取資料庫（ORM 呼叫、SQL 查詢）
- **不需要 Service Layer**：Controller / Handler 直接處理業務邏輯
- **不需要 Mediator / CQRS**：直接呼叫方法，不透過訊息匯流排
- **不需要 Event System**：直接呼叫後續處理邏輯，不發布事件

```
# ✅ Controller 直接處理
@app.post("/users")
def create_user(data: UserCreate):
    user = User(**data.dict())
    db.add(user)
    db.commit()
    return user

# ❌ 不必要的間接層
@app.post("/users")
def create_user(data: UserCreate):
    return user_service.create(data)  # service 裡面也只是做上面那三行
```

## 5. 單一檔案優先

- 若一個類別、函數、常數只在一個地方使用，就放在使用它的那個檔案中
- 只有當多個檔案需要共用時，才抽取到獨立檔案
- Model / Schema 定義可以集中在一個 `models` 檔案中，不需要每個 model 一個檔案
- 工具函數集中在一個 `utils` 檔案中，不需要按功能拆分成 `string_utils`、`date_utils` 等

## 6. 扁平結構

- 資料夾巢狀不超過 **2 層**（例：`src/modules/`，不要 `src/modules/user/services/`）
- 典型結構範例：

```
project/
├── src/
│   ├── main.py          # 進入點
│   ├── models.py         # 所有資料模型
│   ├── routes.py         # 所有路由（或按大功能拆分為少數幾個檔案）
│   ├── utils.py          # 共用工具函數
│   └── config.py         # 設定載入
├── tests/
│   └── test_core.py      # 核心邏輯測試
├── .env
└── README.md
```

- 若檔案數量超過 15 個才考慮建立子資料夾，但仍不超過 2 層

## 7. 測試策略

- **只寫核心邏輯的單元測試**：有計算、判斷、轉換邏輯的函數才需要測試
- **不需要整合測試**：小型專案的 API 端點用手動測試或 Postman 即可
- **不需要 mock**：若測試需要大量 mock 才能跑，代表程式碼可能過度抽象了
- **不追求覆蓋率指標**：測試是為了確保正確性，不是為了數字
- 測試檔案集中在一個 `tests/` 資料夾，不需要鏡像對應原始碼結構

## 8. 錯誤處理

- 使用簡單的 `try-catch`（或語言等效機制）處理預期的錯誤情境
- **不需要全域例外處理中介軟體**
- **不需要自定義例外類別階層**——直接使用語言內建的例外型別
- 錯誤訊息要對人類可讀，包含足夠的除錯資訊
- 對外 API 回傳統一的錯誤格式即可（HTTP status code + message），不需要錯誤碼系統

```
# ✅ 簡單直接
try:
    user = db.query(User).filter(User.id == user_id).one()
except NoResultFound:
    raise HTTPException(status_code=404, detail=f"找不到使用者 {user_id}")
```

## 9. 設定管理

- 直接使用 `appsettings.json`、`.env` 或環境變數
- **不需要 Options Pattern**、不需要強型別設定類別
- **不需要多環境設定檔**（`appsettings.Development.json` 等）——用環境變數覆蓋即可
- 機密資訊（API key、密碼）一律放在 `.env` 或環境變數中，不進版控

```
# ✅ 直接讀取
import os
DATABASE_URL = os.getenv("DATABASE_URL", "sqlite:///./app.db")

# ❌ 過度設計
class DatabaseSettings(BaseSettings):
    url: str
    pool_size: int = 5
    max_overflow: int = 10
    echo: bool = False
    # ... 一堆目前用不到的設定
```

---

## 何時該升級到中型專案原則？

當出現以下任一跡象時，考慮逐步引入中型專案的架構實踐：

- 團隊成員超過 2 人，需要明確的分工邊界
- 同一段業務邏輯在 3 個以上地方重複
- 單一檔案超過 500 行且難以維護
- 需要對接 3 個以上外部服務
- 需要撰寫整合測試來確保系統正確性
