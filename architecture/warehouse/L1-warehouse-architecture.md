# MOD-003: 倉庫與儲位模組 - L1 架構設計說明書

本文件規劃 `MOD-003 (Warehouse Module)` 的層級關係、實體結構以及前端儲位選擇器的實作方式。

---

## 1. 業務領域模型與關係 (Warehouse Domain Model)

本系統的空間儲存結構採用雙層結構：
1. **資材室 (Warehouse)**：有獨立的資材室代碼（如 `WH01`）與名稱（如 `第一資材室`）。
2. **儲位 (StorageLocation)**：儲位歸屬於某一個資材室，有獨立的儲位代碼（如 `LOC-01-A`）與名稱（如 `A排01號架`）。

```
[資材室 (Warehouse)]
        │ (1 : N)
        v
[儲位 (StorageLocation)]
```

---

## 2. 儲位下拉選單連動 (Warehouse Cascading)

在「新增/修改資產」的表單中，使用者可以設定該資產的「預設存放儲位」。
由於儲位數量可能極多，前端必須提供「資材室」與「儲位」的兩層連動下拉選單。

### 前端實作機制
1. 使用者在資產新增/編輯表單中，先選擇「資材室」下拉選單。
2. 「資材室」的 `change` 事件觸發 jQuery AJAX，呼叫 `/Warehouse/GetLocationsByWarehouse?warehouseId=X`。
3. AJAX 回傳該資材室下所有 `IsActive == true` 的儲位清單，並將其動態渲染至「預設儲位」下拉選單中。
4. 若使用者未選擇資材室，則「預設儲位」以下拉選單呈現空值或提示「請先選擇資材室」。

---

## 3. CRUD 功能與 jQuery 輸入驗證

本模組提供完整的「資材室」與「儲位」之新增、修改、停用與清單查詢功能。

### jQuery 驗證規格
* **資材室表單**：
  - `代碼 (Code)`：必填，半形英數字，不可重複。
  - `名稱 (Name)`：必填，限制長度。
* **儲位表單**：
  - `所屬資材室 (Warehouse)`：必填，下拉選單。
  - `代號 (Code)`：必填，半形英數字，不可重複。
  - `名稱 (Name)`：必填，限制長度。

---

## 4. 停用（刪除）之關聯防護 (Soft Delete Integrity)

當使用者點擊「停用」某個資材室或儲位時，後端必須進行業務規則檢查：
* **資材室停用限制**：若該資材室下尚有「啟用中」的儲位，則禁止停用，前端跳出警告提示。
* **儲位停用限制**：若該儲位中尚有「庫存數量 > 0」的資產，或有資產將其設為「預設儲位」，系統應提示警告，並阻斷停用操作。

詳細欄位與 API 請參閱同級目錄之 [L1-warehouse-architecture.json](file:///c:/02Project/AntigravityWorkFlow/my-agentic-project/architecture/warehouse/L1-warehouse-architecture.json)。
