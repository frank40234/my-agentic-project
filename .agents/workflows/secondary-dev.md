---
description: "二次開發工作流。用於對已完成的專案進行功能擴充、修改或重構。讀取既有架構與狀態，僅針對變更部分進行開發。"
---

# Secondary Development Cycle（二次開發工作流）

> 適用於：已由 `/ai-dev-cycle` 完成初次開發的專案，需要新增功能、修改邏輯或重構。

---

## Phase 0: 既有專案掃描

### Step 0.1: 指定專案路徑

詢問使用者：
> 「請輸入要進行二次開發的專案路徑（例如：D:\Projects\erp-system）：」

或從 `workflowProject/` 目錄中列出可用的專案供選擇。

### Step 0.2: 讀取既有狀態

讀取以下檔案以恢復專案記憶：

| 檔案 | 用途 |
|------|------|
| `artifacts/project_config.json` | 專案設定、DB 連線、**已套用的開發規則** |
| `artifacts/system_state.json` | 未完成項目、技術債、擴充點 |
| `architecture/` | 所有既有架構文件（L0 / L1 / L2） |
| `artifacts/task_queue.json` | 檢查是否有未完成任務 |

若 `project_config.json` 不存在，告知使用者此專案不是由 Agent 工作流建立的，建議改用 `/ai-dev-cycle`。

### Step 0.3: 呈現專案現況摘要

向使用者呈現：
> 「📋 專案現況摘要：
> - 專案名稱：{project_name}
> - 專案規模：{scale}
> - 技術棧：{tech_stack}
> - 已套用規則：{active_rules}
> - 已完成模組：X 個
> - 未完成項目：Y 個
> - 技術債：Z 項
> - 已知擴充點：W 個
> - 測試資料庫：{database_status}
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

呼叫 `architect` skill。Architect 讀取**既有架構**後，產生「差異草案」：

1. **新增項目**：需要新增的模組 / 子模組 / 資料表
2. **修改項目**：需要修改的現有模組（標註修改範圍）
3. **影響評估**：對既有功能的影響分析（哪些現有模組可能受影響）
4. **規則變更**：是否需要調整開發規則（例如從 medium 升級為 large）
5. **不寫入任何檔案**

### Step 1.3: 人工確認差異草案（HITL-0a）

**停止**。等待使用者確認：
> 「以上為架構變更差異草案，請確認。輸入 **approve** 開始更新架構文件，或提供修改意見。」

- 若核准 → 進入 Step 1.4
- 若提供意見 → Architect 修改草案後重新呈現

### Step 1.4: 更新架構文件

Architect **更新**既有架構文件（不覆寫無關部分）：
- 修改的模組：更新對應的 L1 / L2 文件（JSON + Markdown）
- 新增的模組：建立新的 L1 / L2 文件
- L0 主架構：更新模組清單與依賴關係
- 若規則有變更，更新 `project_config.json` 的 `active_rules`

> ⚠️ **僅修改有變更的架構文件，既有的未修改模組文件保持不動。**

### Step 1.5: 人工確認更新後的文件（HITL-0b）

**停止**。呈現變更的文件清單（標註新增/修改）：
> 「架構文件已更新，請確認。輸入 **approve** 繼續開發，或提供修改意見。」

- 若核准 → 進入 Phase 2
- 若提供意見 → Architect 修改相關文件後重新呈現

---

## Phase 2: 任務分解（僅針對變更部分）

### Step 2.1: 建立任務佇列

呼叫 `pm-task-manager` skill。PM **比對架構差異**，僅針對「新增與修改」的部分產生任務：

- 新增的模組 → 產生完整的開發任務
- 修改的模組 → 產生修改任務（包含修改範圍與影響說明）
- 每個任務自動附加**回歸測試要求**：既有功能的測試必須持續通過

輸出至 `{project_root}/artifacts/task_queue.json`（追加至現有佇列，不覆蓋歷史任務紀錄），並向使用者展示任務清單。

> 任務 ID 接續上一次開發的編號（例如上次結束於 TASK-012，則從 TASK-013 開始）。

### Step 2.2: 派送第一個任務
取出第一個 pending 任務，進入 Phase 3。

---

## Phase 3: 開發實作

與 `/ai-dev-cycle` Phase 4 相同：

### Step 3.1: 建立 Branch
Coder 從 `main` 建立 `feature/task-{id}` 分支。

### Step 3.2: 實作
Coder 依任務描述、相關架構與**載入的開發規則**實作功能。

**額外注意事項（二次開發特有）**：
- 修改既有程式碼時，必須確保不破壞現有功能
- 若需修改既有的資料表結構，必須建立 Migration（不得直接修改 DB Schema）
- 若修改了公開 API 的回應格式，必須在 commit message 中標註 `BREAKING CHANGE`

### Step 3.3: Commit
Coder 以描述性訊息 commit 變更。

---

## Phase 4: 測試驗證

與 `/ai-dev-cycle` Phase 5 相同，但增加回歸測試要求：

### Step 4.1: 建置與測試
Reviewer 執行建置與測試。**所有既有測試必須持續通過**（回歸驗證）。

### Step 4.2: 結果判斷

**成功**：進入 Phase 5。
**失敗且 retry_count < 3**：送回 Coder 修正。
**失敗且 retry_count >= 3（HITL-2）**：人工介入。

---

## Phase 5: 合併、進度更新與循環

### Step 5.1: 合併分支回 main

```bash
cd {project_root}
git checkout main
git merge --no-ff feature/task-{id} -m "merge(TASK-{id}): 合併 {任務標題}"
git branch -d feature/task-{id}
```

### Step 5.2: 更新任務狀態
PM 更新 `task_queue.json`。

### Step 5.3: 更新進度日誌
Documenter（Mode A）更新 `progress_log.json`。

### Step 5.4: 判斷下一步

- **還有更多任務？** → 返回 Phase 2 Step 2.2
- **全部完成？** → 進入 Phase 6

---

## Phase 6: 文件更新與完成

### Step 6.1: 更新最終文件
呼叫 `documenter` skill（Mode B）：
1. 掃描更新後的程式碼庫
2. **更新**（非覆寫）`{project_root}/README.md`
3. **合併更新** `{project_root}/artifacts/system_state.json`：
   - 移除已解決的技術債與未完成項目
   - 新增本次開發中發現的新技術債
   - 更新擴充點清單
4. 更新 `{project_root}/architecture/ARCHITECTURE-INDEX.md`

### Step 6.2: 測試資料庫處理

若本次開發有新增資料表（透過 Migration），測試資料庫已自動更新，不需額外處理。
若使用者想重置測試資料庫，可在此步驟手動要求。

### Step 6.3: 通知完成
告知使用者：「二次開發完成！」
列出：
- 本次新增/修改的功能
- 變更的檔案清單
- 新增的技術債（若有）
- 被跳過的任務（若有）
