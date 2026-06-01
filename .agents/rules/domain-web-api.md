---
trigger: model_decision
description: "Web API 領域開發規範。涵蓋 RESTful 設計、HTTP 狀態碼、驗證授權、API 版本控制等。"
---

# Web API 領域開發規範

## RESTful URL 設計

- 資源名稱使用**名詞複數形式**：`/api/v1/users`、`/api/v1/orders`
- 巢狀資源**不超過 2 層**：`/api/v1/users/{userId}/orders`（允許），避免 `/api/v1/users/{userId}/orders/{orderId}/items/{itemId}`
- 超過 2 層時，將子資源提升為頂層端點：`/api/v1/order-items?orderId={orderId}`
- 篩選、分頁、排序一律使用 **query string**：
  ```
  GET /api/v1/products?category=electronics&sort=price:asc&offset=0&limit=20
  ```
- URL 路徑使用 **kebab-case**：`/api/v1/user-profiles`（非 `userProfiles`）

## HTTP Method 語意

| Method   | 語意       | 冪等性 | 典型用途                     |
|----------|------------|--------|------------------------------|
| `GET`    | 讀取       | ✅     | 取得資源或資源列表           |
| `POST`   | 建立       | ❌     | 新增資源、觸發非冪等操作     |
| `PUT`    | 完整更新   | ✅     | 以完整物件替換現有資源       |
| `PATCH`  | 部分更新   | ✅     | 僅更新指定欄位               |
| `DELETE` | 刪除       | ✅     | 移除資源                     |

- 禁止使用 `GET` 執行有副作用的操作（如刪除、修改資料）
- `PUT` 必須傳送完整資源表示，缺少的欄位視為清空
- `PATCH` 只傳送需要變更的欄位

## HTTP 狀態碼規範

### 成功（2xx）

| 狀態碼 | 用途                           | 適用 Method          |
|--------|--------------------------------|----------------------|
| `200`  | 請求成功，回傳資料             | GET、PUT、PATCH      |
| `201`  | 資源建立成功，回傳新資源       | POST                 |
| `204`  | 操作成功，無回傳內容           | DELETE                |

### 客戶端錯誤（4xx）

| 狀態碼 | 用途                                       |
|--------|--------------------------------------------|
| `400`  | 請求格式錯誤（JSON 解析失敗、缺少必要欄位）|
| `401`  | 未驗證（缺少或無效的身份驗證憑證）         |
| `403`  | 無權限（身份已驗證但無存取權限）           |
| `404`  | 資源不存在                                 |
| `409`  | 資源衝突（如重複建立、版本衝突）           |
| `422`  | 驗證失敗（格式正確但業務規則不通過）       |

### 伺服器錯誤（5xx）

| 狀態碼 | 用途                                       |
|--------|--------------------------------------------|
| `500`  | 伺服器內部錯誤（未預期例外）               |

- 禁止對所有錯誤一律回傳 `200` 再自訂 error code
- `401` 與 `403` 必須嚴格區分：未登入 vs 已登入但無權限

## 統一回應格式

### 成功回應

```json
{
  "data": { ... },
  "pagination": {
    "offset": 0,
    "limit": 20,
    "totalCount": 150,
    "hasMore": true
  }
}
```

- 單一資源：`data` 為物件
- 資源列表：`data` 為陣列，且包含 `pagination`
- 無內容操作（204）：不回傳 body

### 錯誤回應

```json
{
  "error": {
    "code": "VALIDATION_FAILED",
    "message": "輸入資料驗證失敗",
    "details": [
      { "field": "email", "message": "格式不正確" }
    ]
  }
}
```

- `code`：機器可讀的錯誤代碼（大寫底線分隔）
- `message`：人類可讀的錯誤訊息
- `details`：選填，提供逐欄位的驗證錯誤細節

## 分頁

- 支援兩種策略，依場景選擇：
  - **Offset-based**：`?offset=0&limit=20`，適用於可跳頁的場景
  - **Cursor-based**：`?cursor=abc123&limit=20`，適用於無限捲動、大量資料集
- 回應必須包含 `totalCount`（offset-based）和 `hasMore`
- 預設 `limit` 為 20，最大 `limit` 不超過 100
- 禁止不帶分頁參數的列表端點回傳全部資料

## 驗證與授權

- 驗證方式：使用 **JWT Bearer Token** 或 **Cookie-based Session**
  - JWT 存放於 `Authorization: Bearer <token>` header
  - Token 必須設定合理過期時間，搭配 Refresh Token 機制
- 授權模型：使用 **Role-based（RBAC）** 或 **Policy-based** 授權
  - 在 Controller/Handler 層級標註權限需求
  - 敏感操作需進行資源層級權限檢查（不只看角色）
- **CORS 設定**：
  - 明確列出允許的 Origins，禁止使用 `*` 於正式環境
  - 僅開放必要的 HTTP Methods 和 Headers

## 輸入驗證

- **所有使用者輸入必須驗證**，包含但不限於：
  - 字串長度上下限
  - 數值範圍
  - 格式檢查（email、電話、URL）
  - 列舉值白名單
- 使用 **FluentValidation**（.NET）或等效驗證庫，將驗證邏輯與業務邏輯分離
- 驗證失敗回傳 `422` 並附帶逐欄位錯誤細節
- 禁止信任前端驗證，後端必須獨立驗證

## API 版本控制

- 優先使用 **URL 路徑版本**：`/api/v1/users`、`/api/v2/users`
- 替代方案：使用自訂 Header `Api-Version: 2`
- 版本升級規則：
  - 非破壞性變更（新增欄位）：不需升版
  - 破壞性變更（移除欄位、改變結構）：必須升版
- 舊版本設定明確的棄用時程與通知

## Swagger / OpenAPI 文件

- 所有端點必須有 **API 文件註解**：
  - 端點描述與用途
  - 所有參數說明（path、query、body）
  - 所有可能的回應狀態碼與範例
  - 需要的驗證方式標註
- 開發環境預設啟用 Swagger UI
- 正式環境視安全需求決定是否開放

## 速率限制（Rate Limiting）

- 以下端點必須加入速率限制：
  - 登入 / 註冊端點
  - 密碼重設端點
  - OTP / 驗證碼發送端點
  - 任何涉及外部 API 呼叫的端點
- 實作方式：使用 Token Bucket 或 Sliding Window 演算法
- 超過限制回傳 `429 Too Many Requests`，並包含 `Retry-After` header