# MOD-004: 庫存管理模組 - L1 架構設計說明書

本文件詳細規劃 `MOD-004 (Inventory Module)` 的庫存一覽表設計、庫存交易/異動逻辑，以及前端介面展現。

---

## 1. 業務邏輯與資料異動 (Inventory Logic)

庫存管理模組是系統的核心終端。其職責為記錄**「哪一個資產存放在哪一個儲位，目前有多少數量，以及何時被誰異動過」**。

### 1.1 庫存核心實體 (Inventory Entity)
庫存表 (`Inventories`) 包含：
* `AssetId` (指向資產基本資訊)
* `StorageLocationId` (指向儲位)
* `Quantity` (當前庫存數量，必須大於或等於 0)
* `UpdatedTime` (最後異動時間)
* `UpdatedBy` (最後修改人，從登入 Cookie 中取得)

### 1.2 庫存數量增減機制 (Adjust Quantity)
系統必須提供庫存增減的 API 與介面。在執行增減時：
1. **防併發安全交易**：
   在同一個 Transaction 中使用 SQL Server 的鎖定或 EF Core 樂觀鎖（Optimistic Concurrency）。
2. **防超扣安全檢查**：
   若扣除數量大於現有數量（導致數量 < 0），系統必須拒絕並拋出「庫存不足」異常，確保帳面庫存無負數。
3. **自動時間戳記**：
   `UpdatedTime` 將自動設為 `DateTime.UtcNow` 或 `DateTime.Now`，`UpdatedBy` 設為目前請求之使用者的工號+姓名。

---

## 2. 庫存一覽表查詢介面 (Inventory Query UI)

庫存一覽表為一個唯讀/快速操作的高級管理看板，方便管理者一目了然地掌握全廠資產分布。

### 2.1 視覺與介面特色 (Modern UI)
* **資訊豐富的表格**：使用 Bootstrap 5 做出斑馬紋與懸停效果，並加入響應式排版。
* **狀態標籤**：庫存為 `0` 時顯示灰色「無庫存」標籤；低於警戒值（例如 < 5）時顯示黃色「低庫存」標籤；正常時顯示綠色。
* **搜尋與篩選列**：
  - 「資材室」下拉選單：可只篩選特定資材室的儲位。
  - 「物料編碼/品名/型號/儲位名稱」複合關鍵字輸入框。
  - 前端使用 jQuery 進行即時或非同步查詢，無需複雜的整頁重刷。

### 2.2 呈現欄位
1. **資材室名稱** (Warehouse Name)
2. **儲位名稱/代號** (Storage Location)
3. **物料編碼** (Material Code)
4. **品名名稱** (Item Name)
5. **型號與廠牌** (Model / Brand)
6. **庫存數量** (Quantity)
7. **單位** (Unit)
8. **最後異動時間** (Last Modified Time)
9. **最後異動人** (Updated By)

---

## 3. 前端欄位驗證 (jQuery Validation)

在庫存異動/調整數量的對話框（Modal）中：
* `調整數量 (Quantity)`：必填，必須為整數。
* `驗證限制`：若為扣除操作，輸入值不得大於該項目在該儲位的當前庫存量。

詳細欄位與 API 請參閱同級目錄之 [L1-inventory-architecture.json](file:///c:/02Project/AntigravityWorkFlow/my-agentic-project/architecture/inventory/L1-inventory-architecture.json)。
