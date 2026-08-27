---
description: "所有 Skill 執行前必須完成的前置步驟。此檔為單一事實來源，各 SKILL.md 內嵌相同內容。"
alwaysApply: false
---

# Skill 前置步驟（Preflight）

> 本檔為前置步驟的**單一事實來源**。architect / pm-task-manager / coder / reviewer / documenter
> 的 SKILL.md 內嵌與本檔相同的文字；若需修改，先改本檔再同步各 skill。

## P1. 讀取專案設定

讀取 `artifacts/project_config.json`，取得：

- `project_root`：**所有** git 操作、檔案建立、指令執行皆在此目錄下進行
- `active_rules`：本專案適用的動態規則
- `database`：若不為 `null`，表示此專案有專用測試資料庫

## P2. 載入動態規則

依 `active_rules` 逐一載入：

- `.agents/rules/{active_rules.principles}.md`（規模原則）
- `.agents/rules/{active_rules.language}.md`（語言規範）
- `.agents/rules/{active_rules.domains[*]}.md`（領域規範，可多個）

規則衝突時，以較具體者為準：`domain` > `language` > `principles` > `global-rules`。

## P3. 檔案不存在時的處理

| 情境 | 處理方式 |
|------|---------|
| Architect 在 Phase 0（草案階段）被呼叫 | **不需要** `project_config.json`，直接開始分析。`project_root` 於 Step 4 寫檔前才讀取 |
| 其他 skill 找不到 `project_config.json` | 停止並回報 workflow：「專案尚未初始化，請先完成 Phase 1」 |
| `active_rules` 指向的規則檔不存在 | 停止並回報 workflow，不得自行略過該規則 |

## P4. 回報對象

**任何 skill 都不得直接呼叫另一個 skill。**
所有產出、失敗、需要他人介入的情況，一律回報給呼叫它的 workflow，由 workflow 決定下一步。
