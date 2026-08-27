---
description: "Core rules for the AI Coding Agentic workflow. Defines multi-agent collaboration behavior, file conventions, HITL checkpoints, the task state machine, and safety mechanisms."
alwaysApply: true
---

# Agentic Workflow Rules

## Agent Roles
此專案使用 5 種 Agent 角色：
1. **Architect** - 系統架構設計與文件產出（先呈現草案確認，再寫入檔案）；含二次開發的差異擴充模式
2. **PM** - 任務分解、派送、狀態機維護與 HITL 升級
3. **Coder** - 在隔離 Branch 上進行程式碼實作與測試撰寫
4. **Reviewer** - 規則符合性檢查、建置與測試執行、回歸驗證，回報結構化結果
5. **Documenter** - 事件記錄（Mode A）、最終文件（Mode B）、增量合併更新（Mode B2）

### 協作原則
- **Skill 之間不得互相直接呼叫。** 所有 skill 一律將結果回報給呼叫它的 workflow，
  由 workflow 決定下一步（包含呼叫下一個 skill）。
- 每個 skill 執行前必須完成 [skill-preflight.md](skill-preflight.md) 定義的前置步驟。
- 指令書寫與跨平台轉換依 [shell-conventions.md](shell-conventions.md)。

## HITL 確認點（人工介入點）

| 確認點 | 觸發時機 | 說明 |
|--------|---------|------|
| HITL-0a | 架構草案 / 差異草案產出後 | 確認規劃方向與**開發規則選擇**，此時**尚未寫入任何檔案** |
| HITL-0b | 架構文件寫入後 | 確認已產出或已更新的架構文件內容 |
| HITL-2  | 同一任務連續失敗達 `max_retries` | 強制人工介入除錯或決定跳過；亦用於合併衝突 |

HITL-2 的完整處理規則以 [pm-task-manager](../skills/pm-task-manager/SKILL.md) Step 6 為單一出處，
workflow 文件只引用不重述。

## 檔案規範
所有路徑皆以 `artifacts/project_config.json` 中的 `project_root` 為根目錄。

### 全域設定（所有專案共用）
- DB 伺服器連線：`.agents/config/database.json`（已加入 `.gitignore`，含帳密）
- DB 連線範本：`.agents/config/database.example.json`（版控用，不含真實帳密）

### 專案級檔案

| 路徑 | 格式 | 由誰維護 |
|------|------|---------|
| `{project_root}/.gitignore` | text | workflow Phase 1（第一次 commit 前必須建立） |
| `{project_root}/artifacts/project_config.json` | JSON | workflow Phase 1 建立；`active_rules` 經 HITL-0a 確認 |
| `{project_root}/architecture/` | **Markdown + YAML frontmatter** | architect |
| `{project_root}/artifacts/task_queue.json` | JSON | pm-task-manager |
| `{project_root}/artifacts/progress_log.json` | JSON | documenter（Mode A） |
| `{project_root}/artifacts/system_state.json` | JSON | documenter（Mode B / B2） |
| `{project_root}/architecture/ARCHITECTURE-INDEX.md` | Markdown | documenter（medium/large） |
| `{project_root}/src/`、`{project_root}/tests/` | 依框架慣例 | coder |

### 格式選用原則
- **JSON**：被逐欄位反覆改寫的「狀態」（task_queue / project_config / progress_log / system_state）
- **Markdown + frontmatter**：寫一次後被閱讀理解的「規格」（architecture/）
  - frontmatter 放需要機器精確讀取的欄位（`level`、`priority`、`status`、`dependencies`、`version`）
  - 正文放供人與 AI 閱讀的規格內容
  - **架構文件不再產出 `.json` 版本**，避免雙份文件在局部更新時漂移

## 任務狀態機

```
pending ──▶ in_progress ──▶ in_review ──▶ done
              ▲                  │
              │                  ▼
              └──────────────  failed ──▶ skipped（使用者於 HITL-2 選擇跳過）
```

| 狀態 | 意義 |
|------|------|
| `pending` | 已拆解，尚未派送（含依賴未滿足） |
| `in_progress` | 已派送給 coder，實作中 |
| `in_review` | coder 完成，等待 reviewer 驗證 |
| `done` | reviewer 驗證通過 |
| `failed` | reviewer 驗證未通過，`retry_count += 1` |
| `skipped` | 使用者在 HITL-2 選擇跳過 |

- 達 `max_retries`（預設 3）後任務**維持 `failed`**，這是 `/hotfix` 的進入點，不得改成其他狀態
- 每次狀態變更後立即寫回 `task_queue.json`

## 安全機制
- 同一任務連續失敗達 `max_retries` 次，強制觸發 HITL-2 人工介入
- 每個子 Agent 只載入架構摘要與當前任務上下文
- 前一任務的對話歷史**不得**帶入新任務
- 所有架構變更需人工核准後才能繼續
- **架構草案確認（HITL-0a）必須在寫入任何架構文件前完成**
- **Large 專案的 L2 子模組不需逐一確認，一次全部產出後直接進入開發**

### 機敏資訊
- `project_config.json` 的連線字串使用 `${DB_PASSWORD}` 佔位符，**不得包含明文密碼**
- 實際帳密只存在於 `.agents/config/database.json`（已 gitignore）
- 專案 `.gitignore` 必須在第一次 commit 前建立，並涵蓋 `.env`、`appsettings.Development.json`、
  `artifacts/project_config.json`
- 任何文件、日誌、commit 訊息、agent 回報都不得出現密碼或完整連線字串

## 品質關卡
- **每個任務必須包含自動化測試**；找不到測試時 reviewer 一律回報
  `missing_tests` 失敗，**不得**以 build-only 判定成功
- Coder 必須實際執行測試通過後才能 commit；無法執行測試時回報 `blocked`，不得交付
- 程式碼必須成功建置後才能執行測試（該技術棧無建置階段者除外）
- Reviewer 必須執行規則符合性檢查（`active_rules`），違反者判定失敗
- 所有驗收標準必須通過驗證才能將任務標記為 `done`
- 二次開發任務必須通過回歸驗證：在主線通過的測試不得於任務分支失敗

## 通用型設計原則
- 不預設任何特定技術棧、框架或專案類型
- 技術棧欄位依專案類型動態調整
- 支援的專案類型：後端 API、前端 SPA、全端、行動 App、CLI、資料工程、嵌入式、桌面應用

## 規則選擇機制
每個專案在 `project_config.json` 的 `active_rules` 中記錄適用的規則：

| 類別 | 可選值 | 數量 |
|------|--------|------|
| `principles` | `principles-small` / `principles-medium` / `principles-large` | 必選 1 |
| `language` | `lang-csharp` / `lang-python` / `lang-typescript` / `lang-go` | 必選 1 |
| `domains` | `domain-web-api` / `domain-game-dev` / `domain-erp` | 0 至多選 |

- 規則由 Architect 在 Phase 0 自動推薦（判斷標準見 architect skill Step 2.5），
  經使用者在 **HITL-0a** 確認後，於 Phase 1 寫入 `project_config.json`
- 所需規則檔不存在時，Architect 須在 HITL-0a 一併呈現草擬內容，確認後才建立
- 所有規則檔案位於 `.agents/rules/`，由各 Agent 依 `active_rules` 動態載入
- 規則衝突時以較具體者為準：`domain` > `language` > `principles` > `global-rules`

## Git 分支策略
- 主線分支名稱由 Phase 1 偵測後寫入 `project_config.json` 的 `main_branch`，
  **不得硬編碼 `main`**
- 分支名稱由 PM 產生並寫入 `task_queue.json` 的 `branch_name` 欄位
  （`feature/task-{三位數}`；hotfix 為 `hotfix/task-{三位數}`）
- **任何 workflow 或 skill 都不得自行以 `feature/task-{id}` 組合分支名稱**，
  一律引用 `branch_name` 欄位
- 測試通過後自動合併回主線（`git merge --no-ff`），合併後刪除任務分支
- 合併衝突時 `git merge --abort` 並觸發 HITL-2，不由 agent 自行解決
- 主線分支始終保持可建置、可測試的狀態
