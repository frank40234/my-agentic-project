# MOD-001: 身分驗證與模擬器模組 - L1 架構設計說明書

本文件說明 `MOD-001 (Auth Module)` 的驗證工作流、安全性配置、開發模擬登入邏輯與前端介面。

---

## 1. 模組職責 (Module Responsibility)

由於本系統不需要進行帳號註冊與密碼驗證，本模組的主要職責為：
1. **Cookie 認證配置**：在系統啟動時（`Program.cs`）配置 Cookie 身分驗證攔截器。
2. **路由保護 (Route Protection)**：確保除了登入模擬頁面及未授權提示頁外，所有 Controller 皆掛載 `[Authorize]` 屬性。
3. **異動身分共享 (Claims Extraction)**：為其他業務模組提供目前請求的 `EmployeeNo` (工號) 與 `EmployeeName` (姓名) Claims，以便自動填入建立/修改人。
4. **開發測試模擬器**：在本機開發階段，藉由 `/Auth/Simulator` 提供快速發送符合規格之 Cookie 的頁面。

---

## 2. Cookie 驗證規格 (Cookie Authentication Specs)

* **Cookie 名稱**：`EAM_AUTH_COOKIE`
* **時效 (Expiration)**：`5 分鐘` (絕對時間絕對過期，以防止外系統產生的 Session 在本機持續過久)
* **Claims 結構**：
  * `ClaimTypes.NameIdentifier`：儲存工號 (EmployeeNo)
  * `ClaimTypes.Name`：儲存姓名 (EmployeeName)
* **未授權行為**：
  * 若請求未帶此 Cookie 或已過期，ASP.NET Core 將自動重導向至 `/Auth/AccessDenied`。

---

## 3. 開發模擬登入流程

```
[開發人員/測試者] ──> 開啟瀏覽器 /Auth/Simulator
                           │
                           ├─> 輸入工號與姓名
                           │
                           v
                     送出表單至 /Auth/SimulateLogin
                           │
                           ├─> 後端建立 ClaimsIdentity
                           ├─> 簽發過期時間為 5 分鐘的 EAM_AUTH_COOKIE
                           │
                           v
                     重導向至系統首頁 / (此時具有登入身分)
```

---

## 4. UI/UX 頁面規劃

### 4.1 /Auth/AccessDenied (未授權說明頁)
* **風格**：簡潔美觀的 Bootstrap 5 卡片。
* **內容**：
  - 醒目的警示圖示（紅色或橘色盾牌）。
  - 文字說明：「您目前未授權或登入時效（5分鐘）已過期。請從外部系統點選資產管理連結進入。」
  - 提供「前往開發模擬登入頁」的隱藏或測試連結，方便開發期間測試。

### 4.2 /Auth/Simulator (開發者模擬器)
* **風格**：採用 modern dark card 視覺設計。
* **欄位**：
  - `工號 (Employee No)`：必填，純文字。
  - `姓名 (Employee Name)`：必填，繁體中文。
* **驗證**：jQuery Validation 驗證必填。
* **動作**：點擊「模擬外部登入」後寫入 Cookie 並跳轉。

---

## 5. 程式碼與配置實作要點

```csharp
// Program.cs 偽代碼
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "EAM_AUTH_COOKIE";
        options.LoginPath = "/Auth/AccessDenied"; // 未登入時重導向
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
        options.SlidingExpiration = false; // 絕對 5 分鐘過期，符合用戶要求
    });
```

詳細介面與 API 設計請參閱同級目錄之 [L1-auth-architecture.json](file:///c:/02Project/AntigravityWorkFlow/my-agentic-project/architecture/auth/L1-auth-architecture.json)。
