---
description: "二次開發工作流。用於對已完成的專案進行功能擴充、修改或重構。讀取既有架構與狀態，僅針對變更部分進行開發，並以回歸測試保護既有功能。"
---

# Secondary Development Cycle（二次開發工作流）

> 適用於：已由 `/ai-dev-cycle` 完成初次開發的專案，需要新增功能、修改邏輯或重構。
> 指令語法依 [.agents/rules/shell-conventions.md](../rules/shell-conventions.md)。

---

## Phase 0: 既有專案掃描

### Step 0.1: 指定專案路徑

詢問使用者：
> 「請輸入要進行二次開發的專案路徑（例如：D:\Projects\erp-system）：」

若使用者不確定路徑，請其提供上層目錄，掃描其下含 `artifacts/project_config.json`
的子目錄後列出供選擇。

### Step 0.2: 讀取既有狀態

讀取以下檔案以恢復專案記憶：

| 檔案 | 用途 |
|------|------|
| `artifacts/project_config.json` | 專案設定、`main_branch`、DB 連線、**已套用的開發規則** |
| `artifacts/system_state.json` | 未完成項目、技術債、擴充點、開發失敗歷程 |
| `architecture/` | 所有既有架構文件（Markdown + frontmatter，L0 / L1 / L2） |
| `artifacts/task_queue.json` | 既有任務紀錄與 `last_task_number`；檢查是否有未完成任務 |
| `artifacts/progress_log.json` | 過往開發事件時序（可選，用於了解踩過的坑） |

若 `project_config.json` 不存在，告知使用者此專案不是由 Agent 工作流建立的，建議改用 `/ai-dev-cycle`。

若 `task_queue.json` 中仍有 `failed` 狀態的任務，先告知使用者：
> 「偵測到 {n} 個先前失敗的任務尚未處理，建議先執行 `/hotfix` 處理，或確認要略過後再繼續。」

### Step 0.3: 呈現專案現況摘要

向使用者呈現：
> 「📋 專案現況摘要：
> - 專案名稱：{project_name}
> - 專案規模：{scale}
> - 技術棧：{tech_stack}
> - 已套用規則：{active_rules}
> - 已完成模組：X 個
> - 未完成項目：Y 個
> - 技術債（open）：Z 項
> - 已知擴充點：W 個
> - 測試資料庫：{database_status}
> - 失敗待處理任務：{n} 個
>
> 請描述您要進行的新需求。」

---

## Phase 1: 新需求收集與架構擴充

### Step 1.1: 收集新需求
使用者描述新需求。例如：
- 新增報表模組
- 修改庫存計算邏輯
- 升級驗證機制
- 處理 `system_state.json` 中的技術債

### Step 1.2: 產生差異草案（暫不寫檔）

呼叫 `architect` skill（**Mode B：差異擴充**）。Architect 讀取既有架構後，產生差異草案：

1. **新增項目**：需要新增的模組 / 子模組 / 資料表 / API
2. **修改項目**：需要修改的現有文件與修改範圍
3. **影響評估**：受影響的既有模組、風險等級、建議的回歸測試範圍、是否為破壞性變更、是否需要 Migration
4. **規則變更**：是否需要調整 `active_rules`（例如規模由 medium 升級為 large）
5. **不受影響的部分**：明確列出本次不會被修改的既有模組
6. **不寫入任何檔案**

> 差異草案的完整格式以 [architect skill](../skills/architect/SKILL.md) Step B2 為準。

### Step 1.3: 人工確認差異草案（HITL-0a）

**停止**。等待使用者確認：
> 「以上為架構變更差異草案，請確認。輸入 **approve** 開始更新架構文件，或提供修改意見。」

- 若核准 → 進入 Step 1.4
- 若提供意見 → Architect 修改草案後重新呈現

### Step 1.4: 更新架構文件

Architect（Mode B Step B3）**更新**既有架構文件：
- 修改的模組：只改受影響的章節，其餘章節逐字保留
- 新增的模組：建立新的 L1 / L2 文件
- L0：更新 frontmatter 的 `modules` 與正文的跨模組契約
- 每個被修改的文件 bump `version`、更新 `updated_at`
- 若規則有變更，更新 `project_config.json` 的 `active_rules`

> ⚠️ **僅修改差異草案中列出的文件與章節，其餘內容必須逐字不變。**

### Step 1.5: 人工確認更新後的文件（HITL-0b）

**停止**。呈現變更的文件清單（標註新增/修改，含版本號）：
> 「架構文件已更新，請確認。輸入 **approve** 繼續開發，或提供修改意見。」

- 若核准 → 進入 Phase 2
- 若提供意見 → Architect 修改相關文件後重新呈現

---

## Phase 2: 任務分解（僅針對變更部分）

### Step 2.1: 建立任務佇列

呼叫 `pm-task-manager` skill（**Mode B**）。PM 比對架構差異，僅針對「新增與修改」的部分產生任務：

- 新增的模組 → 產生完整的開發任務
- 修改的模組 → 產生修改任務（`description` 含修改範圍與對既有行為的影響）
- 資料表結構變更 → 產生獨立的 Migration 任務，排在依賴它的任務之前
- **每個任務都必須填寫 `regression_scope`**（受影響的既有測試範圍）；
  未填寫者不得派送

輸出至 `{project_root}/artifacts/task_queue.json`：**讀入既有檔案後追加**，
不覆蓋歷史任務紀錄；任務 ID 從 `last_task_number + 1` 接續編號。
產生後向使用者展示任務清單。

### Step 2.2: 派送任務
呼叫 PM 的「派送下一個任務」，由 PM 依依賴關係挑選並將 `status` 設為 `in_progress`，
進入 Phase 3。

---

## Phase 3: 開發實作

與 `/ai-dev-cycle` Phase 4 相同：

### Step 3.1: 建立 Branch
Coder 從 `{main_branch}` 建立任務物件中 `branch_name` 指定的分支（不得自行組合名稱）。

### Step 3.2: 實作
Coder 依任務描述、相關架構、`regression_scope` 與**載入的開發規則**實作功能。

**二次開發專屬要求**（完整規則見 [coder skill](../skills/coder/SKILL.md)「二次開發專屬要求」）：
- 修改既有程式碼時，必須確保不破壞現有功能
- 既有測試不得刪除或跳過；因合理行為變更需調整者，必須在回報的 `concerns` 記錄原因
- 需修改既有資料表結構時，必須建立 Migration（不得直接改動 DB Schema）
- 修改公開 API 的回應格式時，commit message 必須標註 `BREAKING CHANGE`

### Step 3.3: Commit 與回報
Coder 只加入任務範圍內的檔案，以描述性訊息 commit，並以結構化格式回報 workflow。
PM 將 `status` 更新為 `in_review`。

---

## Phase 4: 測試驗證

與 `/ai-dev-cycle` Phase 5 相同，但**必須執行回歸驗證**：

### Step 4.1: 建置與測試
Reviewer 依 `regression_scope` 執行回歸驗證：先在 `{main_branch}` 建立測試基準清單，
再於任務分支執行測試並比對。**任何在主線通過、在任務分支失敗的測試**都判定為
`regression_failure`。

### Step 4.2: 結果判斷

- **成功** → 進入 Phase 5
- **失敗且 `retry_count < max_retries`** → PM 設為 `failed` 並遞增 `retry_count`，
  帶錯誤摘要重新派送 Coder
- **失敗且 `retry_count >= max_retries`（HITL-2）** → 任務**維持 `failed`**，人工介入
  （規則見 [pm-task-manager](../skills/pm-task-manager/SKILL.md) Step 6）

---

## Phase 5: 合併、進度更新與循環

### Step 5.1: 合併分支回主線

```bash
cd {project_root}
git checkout {main_branch}
git merge --no-ff {branch_name} -m "merge(TASK-{id}): 合併 {任務標題}"
git branch -d {branch_name}
```

合併衝突時：`git merge --abort` 後直接觸發 HITL-2，不由 agent 自行猜測解法。

### Step 5.2: 更新任務狀態
PM 將任務標記為 `done` 並寫入 `completed_at`。

### Step 5.3: 記錄開發事件
由 **workflow** 呼叫 `documenter`（Mode A）追加事件至 `progress_log.json`。

### Step 5.4: 判斷下一步

- **還有更多任務？** → 返回 Phase 2 Step 2.2
- **全部完成？** → 進入 Phase 6

---

## Phase 6: 文件更新與完成

### Step 6.1: 更新最終文件
呼叫 `documenter` skill（**Mode B2：增量合併更新**）：
1. 掃描更新後的程式碼庫
2. **逐節合併更新** `{project_root}/README.md`（未受影響的章節逐字保留）
3. **合併更新** `{project_root}/artifacts/system_state.json`：
   - 已解決的技術債將 `status` 改為 `resolved`（保留紀錄，不刪除）
   - 已完成的未完成項目移除；本次新跳過者追加
   - 新增本次發現的技術債與擴充點
   - 累加本次的失敗與重試紀錄
   - `meta.version` 次版號 +0.1
4. 更新 `{project_root}/architecture/ARCHITECTURE-INDEX.md`

> 合併規則的完整定義見 [documenter skill](../skills/documenter/SKILL.md) Mode B2。

### Step 6.2: 測試資料庫處理

若本次開發有新增資料表（透過 Migration），測試資料庫已於開發過程中更新，不需額外處理。
若使用者想重置測試資料庫，可在此步驟提出，由 workflow 執行 DROP / CREATE 後重跑 Migration。

### Step 6.3: 通知完成
告知使用者：「二次開發完成！」
列出：
- 本次新增/修改的功能
- 變更的檔案清單
- 新增與已解決的技術債
- 被跳過的任務（若有）
- 破壞性變更（若有），並提醒下游使用者
