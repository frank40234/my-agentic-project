---
name: documenter
description: >
  文件撰寫 Agent。負責兩項工作：(1) 即時更新進度日誌 progress_log.json，
  (2) 專案結案時生成 README.md 和 system_state.json。
  當任務完成需要記錄進度，或所有任務結束需要生成文件時使用此技能。
---

# Documenter 技能指南

## 角色
你是一位技術文件撰寫者。你有兩種工作模式：

---

## 模式 A：即時進度紀錄（背景執行）

當收到 `PM` 的「任務完成」通知時：

1. 讀取 `artifacts/progress_log.json`
2. 追加新的完成紀錄
3. 寫回檔案

### `progress_log.json` 格式：
```json
{
  "project_name": "...",
  "last_updated": "2026-05-28T15:00:00Z",
  "completed_tasks": [
    {
      "task_id": "TASK-001",
      "title": "建立 User 資料模型",
      "completed_at": "2026-05-28T14:30:00Z",
      "branch": "feature/task-001",
      "retries": 0
    }
  ],
  "pending_tasks": 3,
  "total_tasks": 5
}
```
⚠️ 此模式不應阻礙主流程，更新完畢後立即回到 `Idle` 狀態。

## 模式 B：結案文件生成（所有任務完成後）

### Step 1：全域掃描
掃描最終程式碼庫，了解：

- 專案結構
- 所有已實作的功能
- 使用的技術棧
- `API Endpoints`

### Step 2：生成 `README.md`（人類閱讀用）
包含以下章節：

- 專案簡介
- 系統架構概覽
- 安裝指南（前置需求、安裝步驟）
- 設定說明（環境變數、設定檔）
- 部署指南
- `API` 文件摘要
- 開發指南（如何新增功能、執行測試）

### Step 3：生成 `system_state.json`（AI 閱讀用）
```json
{
  "version": "1.0",
  "generated_at": "...",
  "architecture_decisions": [...],
  "dependency_map": {...},
  "file_structure": {...},
  "api_endpoints": [...],
  "database_schema": [...],
  "test_coverage_summary": {...},
  "known_issues": [],
  "future_improvements": []
}
```
### Step 4：通知完成
告知使用者：「專案開發完畢，文件已生成。」

[Antigravity 技能指南](https://antigravity.google/docs/skills) | [Medium 技能實務指南](https://medium.com/google-cloud/tutorial-getting-started-with-antigravity-skills-864041811e0d)

---

## <a name="step5"></a>Step 5：建立主控 Workflow（參考範例）

`Workflow` 是可透過斜杠指令 `/workflow-name` 觸發的流程腳本。[Antigravity 工作流指南](https://antigravity.google/docs/rules-workflows)

### 5.1 建立主工作流

**檔案：`.agents/workflows/ai-dev-cycle.md`**

```markdown
---
description: >
  完整的 AI Coding Agentic 開發週期。包含六個階段：架構設計 → 任務拆解 → 
  開發 → 測試 → 進度追蹤 → 結案文件。含 HITL 中斷點與異常處理。
  當要啟動一個完整的自動化開發流程時使用。
---

# AI Coding Agentic 完整開發週期

## 階段 1：需求定義與架構審查

### Step 1.1：收集需求
請使用者描述專案目標與需求。如果需求不夠明確，使用 `/grill-me` 風格的提問逐步釐清。

### Step 1.2：生成架構
使用 `architect` 技能，分析需求並生成 `artifacts/architecture.json`。
包含：資料庫 `Schema`、`API` 規格、依賴模組、架構決策紀錄。

### Step 1.3：人類審查（`HITL-1`） ⚠️
**停下來！** 將架構書的摘要呈現給使用者。
明確詢問：「請審查架構書。輸入 'approve' 放行，或提供修改建議。」
- 如果使用者回覆 `"approve"` → 進入階段 2
- 如果使用者提供修改建議 → 回到 Step 1.2 修改後重新審查

---

## 階段 2：任務拆解與分派

### Step 2.1：建立任務佇列
使用 `pm-task-manager` 技能，讀取已核准的架構書，拆解為獨立開發任務。
輸出 `artifacts/task_queue.json`。
向使用者展示任務清單。

### Step 2.2：開始分派
從佇列中提取第一項任務，進入階段 3。

---

## 階段 3：隔離開發與版本控制

### Step 3.1：建立分支
使用 `coder` 技能，為當前任務建立獨立 `Git` 分支：`feature/task-{id}`

### Step 3.2：撰寫程式碼
`Coder` 依據任務描述與架構書撰寫程式碼。
⚠️ **記憶管理**：只載入架構摘要 + 當前任務目標，不載入前一任務的上下文。

### Step 3.3：提交變更
執行 `git add .` 與 `git commit`，進入階段 4。

---

## 階段 4：沙盒測試與判定

### Step 4.1：環境建置與測試
使用 `reviewer` 技能，在當前分支執行編譯與測試指令。

### Step 4.2：結果判定
- **失敗**（`exit_code != 0`）：
  - 記錄 `retry_count`
  - 如果 `retry_count` < 3：將錯誤日誌傳回 `PM`，`PM` 產出修正指令，退回 Step 3.2
  - 如果 `retry_count` >= 3：**HITL-2 中斷** ⚠️ 停下來告知使用者：
    「任務 {id} 連續失敗 3 次。最後錯誤：{error}。請提供除錯方向，或輸入 'skip' 跳過。」
- **成功**（`exit_code == 0`）：進入階段 5

---

## 階段 5：進度紀錄與迴圈推進

### Step 5.1：標記完成
`PM` 標記當前任務為 `"done"`。

### Step 5.2：更新進度
使用 `documenter` 技能（模式 A），更新 `artifacts/progress_log.json`。

### Step 5.3：佇列檢查
- 如果還有剩餘任務 → 回到階段 2 的 Step 2.2，提取下一項任務
- 如果佇列已清空 → 進入階段 6

---

## 階段 6：結案與文件生成

### Step 6.1：全域掃描
使用 `documenter` 技能（模式 B），掃描最終程式碼庫。

### Step 6.2：生成文件
- 生成 `README.md`（人類閱讀用）
- 生成 `artifacts/system_state.json`（AI 閱讀用）

### Step 6.3：通知完成
告知使用者：「🎉 專案開發完畢！請查看 `README.md` 與 `artifacts/system_state.json`。」