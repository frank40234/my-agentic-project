---
description: "Core rules for the AI Coding Agentic workflow. Defines multi-agent collaboration behavior, file conventions, HITL checkpoints, and safety mechanisms."
alwaysApply: true
---

# Agentic Workflow Rules

## Agent Roles
此專案使用 5 種 Agent 角色：
1. **Architect** - 系統架構設計與文件產出（先呈現草案確認，再寫入檔案）
2. **PM** - 任務分解、派送、進度管理與 HITL 升級
3. **Coder** - 在隔離 Branch 上進行程式碼實作
4. **Reviewer** - 建置與測試執行，回報結構化結果
5. **Documenter** - 進度追蹤（Mode A）與最終文件產出（Mode B）

## HITL 確認點（人工介入點）

| 確認點 | 觸發時機 | 說明 |
|--------|---------|------|
| HITL-0a | 架構草案產出後 | 確認規劃方向，此時**尚未寫入任何檔案** |
| HITL-0b | 架構文件寫入後 | 確認已產出的架構文件內容 |
| HITL-2  | 同一任務連續失敗 3 次 | 強制人工介入除錯或決定跳過 |

## 檔案規範
所有路徑皆以 `artifacts/project_config.json` 中的 `project_root` 為根目錄。

- 專案設定：`{project_root}/artifacts/project_config.json`
- 架構文件：`{project_root}/architecture/`
  - 每個 JSON 架構文件都必須有對應的 `.md` 人類可讀版本
- 任務佇列：`{project_root}/artifacts/task_queue.json`
- 進度日誌：`{project_root}/artifacts/progress_log.json`
- 最終系統狀態：`{project_root}/artifacts/system_state.json`（含未完成功能、技術債、擴充點）
- 架構索引：`{project_root}/architecture/ARCHITECTURE-INDEX.md`（medium/large 專案）
- 原始碼：`{project_root}/src/`（或框架慣例）
- 測試：`{project_root}/tests/`（或框架慣例）

## 安全機制
- 同一任務連續失敗 3 次，強制觸發 HITL-2 人工介入
- 每個子 Agent 只載入架構摘要與當前任務上下文
- 前一任務的對話歷史**不得**帶入新任務
- 所有架構變更需人工核准後才能繼續
- **架構草案確認（HITL-0a）必須在寫入任何架構文件前完成**
- **Large 專案的 L2 子模組不需逐一確認，一次全部產出後直接進入開發**

## 品質關卡
- 每個任務必須包含自動化測試
- 程式碼必須成功建置後才能執行測試
- 所有驗收標準必須通過驗證才能將任務標記為完成

## 通用型設計原則
- 不預設任何特定技術棧、框架或專案類型
- 技術棧欄位依專案類型動態調整
- 支援的專案類型：後端 API、前端 SPA、全端、行動 App、CLI、資料工程、嵌入式、桌面應用
