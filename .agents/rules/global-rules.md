---
description: "Global behavior rules for all agents. Applies to every conversation and task."
alwaysApply: true
---

# Global Agent Rules

## Language
- All agent responses, code comments, commit messages, and documentation output must be in **Traditional Chinese (繁體中文)**
- Variable names, function names, class names, and file names must use **English**
- 架構文件 frontmatter 的 key 必須使用 **English**；值可為繁體中文

## Code Style
- Every public function/method/class must have a docstring or XML doc comment
- Follow .editorconfig if present in the project
- Do NOT introduce any third-party package not listed in the architecture document
- Use meaningful variable and function names (no single-letter variables except loop counters)
- 只做任務要求的事：不因為「順手」而重構、加抽象或加未來可能用得到的功能

## Security
- NEVER hardcode secrets, API keys, passwords, or connection strings in source code
- 資料庫密碼只存在於 `.agents/config/database.json`（已 gitignore）；
  `project_config.json` 一律使用 `${DB_PASSWORD}` 佔位符
- 寫入任何含連線資訊的本機設定檔（`.env`、`appsettings.Development.json`）**之前**，
  必須先確認專案 `.gitignore` 已涵蓋該檔
- 密碼、Token 不得出現在 commit 訊息、日誌、文件或 agent 回報內容中
- All user inputs must be validated and sanitized（驗證做在系統邊界：
  外部輸入、外部 API、跨進程邊界、資料庫存取）
- Use parameterized queries for all database operations
- 遵循 OWASP Top 10：避免 SQL / command / XSS 注入
- 發現既有程式碼有安全疑慮但不在本次任務範圍內，記錄到
  `artifacts/system_state.json` 的 `technical_debt`，不擅自修改範圍外程式碼

## Testing
- 每個任務必須交付可驗證其正確性的測試；沒有測試的任務不得標記為 `in_review`
- Coder 必須實際執行測試通過後才能 commit；無法執行時回報 `blocked`
- 既有測試不得刪除或跳過來讓 build 通過；因合理行為變更需調整者，
  必須在該任務回報的 `concerns` 中記錄原因
- 二次開發：所有既有測試必須持續通過（回歸測試）

## Git Conventions
- 分支名稱由 PM 產生並寫入 `task_queue.json` 的 `branch_name`：
  `feature/task-{三位數}`（一般開發）、`hotfix/task-{三位數}`（`/hotfix` 流程）
- **不得自行以 `feature/task-{id}` 組合分支名稱**（`id` 含 `TASK-` 前綴，會組出錯誤名稱），
  一律引用 `branch_name` 欄位
- 主線分支以 `project_config.json` 的 `main_branch` 為準，不得硬編碼 `main`
- Commit message format: `feat(TASK-{id}): 簡短中文描述`；
  破壞性變更必須在訊息中標註 `BREAKING CHANGE`
- **禁止 `git add .`**：只加入本任務範圍內的檔案
- Each task may only modify files within its defined scope
- Always commit on the task branch, never directly on the main branch
- 除非使用者明確要求，不對主線做 force push、`reset --hard` 或跳過 hook

## Error Handling
- All API endpoints must return consistent error response format
- All exceptions must be logged with sufficient context
- Never expose internal error details to end users
- 不為內部呼叫路徑上不可能發生的情境加防禦性檢查

## File Organization
- 架構文件放 `architecture/`（Markdown + YAML frontmatter，不產出 `.json` 版本）
- Task artifacts 放 `artifacts/`（JSON）
- Source code 放 `src/`（或框架慣例）
- Tests 放 `tests/`（或框架慣例）
- README / 使用文件只由 documenter 在文件階段統一產出或合併更新，
  coder 不在個別 task 中零散修改
