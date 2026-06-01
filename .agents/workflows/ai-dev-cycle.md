---
description: "Universal AI Coding Agentic development cycle. Automatically adapts to any project type and scale (small/medium/large). Presents draft plan for human approval BEFORE writing architecture files. Includes HITL checkpoints, retry logic, and error escalation."
---

# Universal AI Coding Agentic Development Cycle

---

## Phase 0: 需求收集與架構規劃

### Step 0.1: 收集需求
請使用者描述專案。**不預設**任何特定技術、框架或專案類型。
若需求模糊或不完整，提出澄清問題。

### Step 0.2: 產生架構草案（暫不寫檔）
呼叫 `architect` skill。Architect 將：
1. 分析需求
2. 判斷專案規模（small / medium / large）
3. 決定專案名稱（英文小寫 + 連字號）
4. 根據規模與技術棧，**自動推薦適用的開發規則**：
   - 規模原則：`principles-{small|medium|large}`
   - 語言規範：`lang-{csharp|python|typescript|go}`
   - 領域規範：`domain-{web-api|game-dev}`（可選，視專案類型而定）
5. 以**純文字摘要**呈現規劃草案，包含：
   - 專案名稱、模組清單、技術決策、預計文件清單
   - **📐 自動套用的開發規則**（列出推薦的規則檔名與說明）
6. **不寫入任何檔案**

### Step 0.3: 人工確認草案（HITL-0a）
**停止**。Architect 已呈現草案，等待使用者確認：
> 「以上為架構規劃草案（含開發規則推薦），請確認。輸入 **approve** 開始建立專案，或提供修改意見。」

- 若核准 → 進入 Phase 1
- 若提供意見（含調整規則選擇）→ Architect 修改草案後重新呈現

---

## Phase 1: 專案初始化（草案確認後執行）

> ⚠️ 此階段必須在 HITL-0a 核准後才能執行。

### Step 1.1: 確認專案位置

詢問使用者：

> 「專案 **{project_name}** 要建立在哪裡？
> A）指定完整路徑（例如：D:\Projects\my-app）
> B）在當前目錄下自動建立 `{project_name}` 子目錄
>
> 請輸入路徑（選 A），或輸入 **B** 使用當前目錄：」

- 若選 A：`project_root` = 使用者提供的完整路徑
- 若選 B：`project_root` = `{當前目錄}/{project_name}`

詢問是否初始化 Git：
> 「是否在此目錄初始化 Git Repository？（預設 Yes）」

### Step 1.2: 建立專案根目錄

```bash
# 建立目錄（若不存在）
mkdir -p {project_root}

# 初始化 Git（若使用者同意）
cd {project_root}
git init
git commit --allow-empty -m "chore: 初始化專案儲存庫"
```

### Step 1.3: 寫入專案設定檔

在 `{project_root}/artifacts/project_config.json` 寫入：

```json
{
  "project_name": "{project_name}",
  "project_root": "{絕對路徑}",
  "git_initialized": true,
  "created_at": "ISO-8601 timestamp",
  "active_rules": {
    "principles": "principles-{small|medium|large}",
    "language": "lang-{language}",
    "domains": ["domain-{domain}"]
  },
  "database": null
}
```

> `active_rules` 來自 HITL-0a 核准的草案。所有 Agent（Coder、Reviewer）在前置步驟中讀取此欄位，並載入對應的 `.agents/rules/` 規則檔。

### Step 1.4: 建立測試資料庫（若專案需要）

檢查已核准的架構草案中 `tech_stack.database` 是否有指定資料庫。

**若專案不需要資料庫**（前端 SPA、CLI 工具等）：跳過此步驟，`database` 維持 `null`。

**若專案需要資料庫**：

1. 讀取 `.agents/config/database.json` 取得 MSSQL 伺服器連線資訊
2. 若檔案不存在或密碼仍為預設值，**停止**並告知使用者：
   > 「需要資料庫但尚未設定連線資訊。請編輯 `.agents/config/database.json` 填入 MSSQL 伺服器帳密，完成後輸入 **done** 繼續。」
3. 建立測試資料庫：

```bash
# 資料庫命名規則：agent_test_{project_name}（連字號替換為底線）
sqlcmd -S {server},{port} -U {user} -P {password} -Q "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'agent_test_{project_name}') CREATE DATABASE [agent_test_{project_name}]"
```

4. 更新 `project_config.json` 的 `database` 欄位：

```json
{
  "database": {
    "enabled": true,
    "name": "agent_test_{project_name}",
    "server": "{server}",
    "port": 1433,
    "connection_string": "Server={server},{port};Database=agent_test_{project_name};User Id={user};Password={password};TrustServerCertificate=True"
  }
}
```

> ⚠️ **連線字串僅存在 `project_config.json` 中，Coder 必須從此檔讀取，禁止硬編碼至原始碼。**

### Step 1.5: 產生架構文件

Architect 依已核准的草案，將所有架構文件寫入 **`{project_root}/architecture/`** 目錄：
- Small：產生單一 `architecture.json` + `architecture.md`
- Medium：產生 L0 + 各模組 L1（JSON + Markdown）
- Large：產生 L0 + 各模組 L1 + 各模組所有 L2（JSON + Markdown），**同一模組的 L2 一次全部產出**

### Step 1.6: 人工確認文件（HITL-0b）
**停止**。呈現已產生的文件清單摘要（若有建立資料庫，一併列出）：
> 「架構文件已產生至 `{project_root}/architecture/`，請確認。輸入 **approve** 繼續開發，或提供修改意見。」

- 若核准 → 進入 Phase 2（Medium/Large）或 Phase 3（Small）
- 若提供意見 → Architect 修改相關文件後重新呈現

> ⚠️ **所有後續產生的檔案（artifacts、源碼、測試）皆以 `project_root` 為根目錄。**

---

## Phase 2: 模組選取（僅 Medium / Large 專案）

Small 專案直接跳至 Phase 3。

### Step 2.1: 選取下一個模組
讀取 `{project_root}/architecture/L0-master-architecture.json`。找到下一個符合條件的模組：
- status 為 `"pending"`
- 所有依賴模組的 status 為 `"done"`

> Large 專案的 L2 子模組設計已於 Step 1.5 一次全部產出，此處**不需要**額外確認，直接進入任務分解。

---

## Phase 3: 任務分解

### Step 3.1: 建立任務佇列
呼叫 `pm-task-manager` skill，將當前範圍分解為開發任務：
- Small：將單一架構拆分成任務
- Medium/Large：將當前模組的 L1（與 L2）拆分成任務
輸出至 `{project_root}/artifacts/task_queue.json`，並向使用者展示任務清單。

### Step 3.2: 派送第一個任務
取出第一個 pending 任務，進入 Phase 4。

---

## Phase 4: 開發實作

### Step 4.1: 建立 Branch
呼叫 `coder` skill。Coder 讀取 `artifacts/project_config.json` 取得 `project_root` 與 `active_rules`，載入對應的規則檔案，並在 `project_root` 下建立隔離的 Git branch。

### Step 4.2: 實作
Coder 依任務描述、相關架構與**載入的開發規則**實作功能。

**記憶體管理原則**：只提供 Coder：
- 與此任務相關的架構摘要
- 當前任務細節
- 適用的開發規則檔案內容
- 若為重試，提供錯誤日誌

**不得**提供其他任務的上下文或不相關模組的架構。

### Step 4.3: Commit
Coder 以描述性訊息 commit 變更。

---

## Phase 5: 測試驗證

### Step 5.1: 建置與測試
呼叫 `reviewer` skill。Reviewer 讀取 `artifacts/project_config.json` 取得 `project_root`，從技術棧自動判斷正確的建置 / 測試指令，並在 `project_root` 目錄下執行。

### Step 5.2: 結果判斷

**成功（exit_code == 0）**：
進入 Phase 6。

**失敗（exit_code != 0）且 retry_count < 3**：
PM 分析錯誤，產生修正指示，將任務送回 Coder。
返回 Phase 4 Step 4.2。

**失敗且 retry_count >= 3（HITL-2）**：
**停止**。告知使用者：
> 「任務 {id}「{title}」已連續失敗 {retry_count} 次。
> 最後錯誤：{error_summary}
> 請提供除錯指引，或輸入 **skip** 跳過此任務。」

- 若使用者提供指引：重置 retry_count，套用指引，返回 Phase 4
- 若使用者輸入 `skip`：標記任務為 skipped，進入 Phase 6

---

## Phase 6: 合併、進度更新與循環

### Step 6.1: 合併分支回 main

若任務狀態為 `"done"`（測試通過），自動將任務分支合併回 `main`：

```bash
cd {project_root}
git checkout main
git merge --no-ff feature/task-{id} -m "merge(TASK-{id}): 合併 {任務標題}"
git branch -d feature/task-{id}
```

若合併發生衝突：
1. Agent 嘗試自動解決衝突
2. 若無法自動解決，觸發 HITL-2 請使用者介入

若任務狀態為 `"skipped"`，跳過合併。

### Step 6.2: 更新任務狀態
PM 將當前任務標記為 `"done"`（或 `"skipped"`）於 `{project_root}/artifacts/task_queue.json`。

### Step 6.3: 更新進度日誌
呼叫 `documenter` skill（Mode A）更新 `{project_root}/artifacts/progress_log.json`。
此為非同步背景操作，不需等待完成。

### Step 6.4: 判斷下一步
檢查剩餘項目：

- **當前模組 / 專案還有更多任務？**
  → 返回 Phase 3 Step 3.2 派送下一個任務

- **當前模組完成，還有更多模組？（Medium/Large）**
  → 在 L0 中將模組狀態更新為 `"done"`
  → 返回 Phase 2 Step 2.1 選取下一個模組

- **全部完成？**
  → 進入 Phase 7

---

## Phase 7: 文件產出與完成

### Step 7.1: 產生最終文件
呼叫 `documenter` skill（Mode B），以 `project_root` 為根目錄：
1. 掃描最終程式碼庫
2. 產生 `{project_root}/README.md`
3. 產生 `{project_root}/artifacts/system_state.json`（含未完成項目、技術債、擴充點）
4. 產生 `{project_root}/architecture/ARCHITECTURE-INDEX.md`（medium/large）

### Step 7.2: 測試資料庫處理

若 `project_config.json` 中 `database.enabled` 為 `true`：

詢問使用者：
> 「測試資料庫 `{database_name}` 要如何處理？
> A）**保留**（供後續二次開發或手動測試使用）
> B）**刪除**（執行 DROP DATABASE）
>
> 請選擇 A 或 B：」

- 若選 A：在 `system_state.json` 中記錄資料庫名稱與連線資訊，供未來 AI 二次開發使用
- 若選 B：

```bash
sqlcmd -S {server},{port} -U {user} -P {password} -Q "DROP DATABASE [{database_name}]"
```

### Step 7.3: 通知完成
告知使用者：「專案開發完成！」
列出所有產生的文件，並標示任何被跳過的任務或已知問題。
若測試資料庫已保留，提醒使用者資料庫名稱與連線方式。
