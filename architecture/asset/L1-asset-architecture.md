# MOD-002: 資產分類與主檔模組 - L1 架構設計說明書

本文件詳細規劃 `MOD-002 (Asset Module)` 的層級關係、資產主檔資料表設計、物料編碼（物料編碼）自動產生規則，以及前端連動選單。

---

## 1. 業務領域模型與關係 (Domain Model)

系統的資產分類關係由上而下為嚴格的樹狀階層：
1. **大類 (MajorCategory)**：例如大類代號 `CH` (儀器設備)。
2. **次類 (MinorCategory)**：歸屬於大類，例如次類代號 `L1` (實驗室儀器)。
3. **品名 (ItemName)**：歸屬於次類，例如品名代碼 `001` (顯微鏡)。
4. **單位 (Unit)**：獨立的主檔，如 `台`、`個`、`套`。
5. **資產基本資訊 (Asset)**：資產主檔，歸屬於品名，並設定一組單位，以及預設存放的儲位。

```
[大類 (MajorCategory)]
        │ (1 : N)
        v
[次類 (MinorCategory)]
        │ (1 : N)
        v
[品名 (ItemName)] <───────+ (N : 1)
                           │
                           v
                    [資產 (Asset)] <────── [單位 (Unit)] (N : 1)
                           │
                           +-------------> [儲位 (StorageLocation)] (N : 1, 預設存放)
```

---

## 2. 物料編碼自動產生規則 (Material Code Generator)

### 2.1 編碼格式
`[大類代碼]-[次類代碼]-[品名代碼]-[4碼流水號]`
範例：`CH-L1-001-0001`

### 2.2 算號邏輯與併發安全機制
1. 用戶在表單上選擇了「品名Id (ItemNameId)」。
2. 後端服務 `AssetService` 透過 `ItemNameId` 取得品名資料，並向上一路 Inner Join 查出其次類代碼、大類代碼。
3. **資料庫鎖定防衝突**：
   - 在同一個 Transaction 中，執行查詢：
     `SELECT MAX(MaterialCode) FROM Assets WITH (UPDLOCK, HOLDLOCK) WHERE ItemNameId = @ItemNameId AND IsActive = 1`
   - 解析出當前最大的流水號（末 4 碼，如 `0003`），將其加 1。若沒有歷史資料，則從 `0001` 開始。
   - 格式化為 4 位字串並重新組合（如 `CH-L1-001-0004`）。
4. **資料庫 Unique Constraint 阻斷**：
   - 資料庫設定 `Assets` 表之 `MaterialCode` 為 `Unique`（可選與 `IsActive` 做複合唯一）。
   - 若發生併發新增且鎖定未成功防堵時，資料庫會拋出 Unique Key 違規異常。後端攔截此異常，並重試該算號程序（最多重試 3 次）。

---

## 3. 前端互動與連動下拉選單 (Cascading Dropdowns)

在「新增/修改資產」或「新增次類/品名」表單中，為了提供順暢的引導，必須實作**連動下拉選單**。

### 連動行為
1. **大類變動**：觸發 jQuery `change` 事件，以 AJAX 呼叫 `/Asset/GetMinorCategoriesByMajor?majorCategoryId=X`，並將取得的資料填入「次類」下拉選單，同時清空「品名」下拉選單。
2. **次類變動**：觸發 jQuery `change` 事件，以 AJAX 呼叫 `/Asset/GetItemNamesByMinor?minorCategoryId=Y`，並將取得的資料填入「品名」下拉選單。

### jQuery Validation 欄位驗證
* **必填欄位**：大類、次類、品名、單位、型號、廠牌。
* **驗證規則**：
  - 各階層代碼（Code）：限半形英數字、減號，不可重複（前端在送出前可非同步檢查或由後端回傳 API 錯誤）。
  - 型號/廠牌：限制長度，不可空白。

---

## 4. 追蹤與軟刪除機制

* 所有實體繼承 `BaseEntity`，並加入 EF Core 全域查詢過濾器，自動隱藏 `IsActive = false` 的停用資料。
* 新增與修改時，由 `AppDbContext` 自動填入 `CreatedBy` 與 `UpdatedBy`（取自 `EAM_AUTH_COOKIE` 夾帶之工號與姓名）。

詳細欄位定義請參閱同級目錄之 [L1-asset-architecture.json](file:///c:/02Project/AntigravityWorkFlow/my-agentic-project/architecture/asset/L1-asset-architecture.json)。
