# my-agentic-project

一套**通用型 AI Coding Agentic 工作流框架**。它本身不含應用程式碼，只定義一組
workflow、agent skill 與開發規則，讓 AI Coding Agent 能以「架構 → 任務 → 開發 → 測試 → 合併 → 文件」
的固定節奏，替**任何技術棧、任何規模**的專案產出可維護的成果。

## 目錄結構

```
.agents/
  workflows/          三個主流程（使用者入口）
    ai-dev-cycle.md     Phase 0-7 主開發流程
    secondary-dev.md    二次開發（含回歸測試）
    hotfix.md           人工除錯後重跑失敗任務
  skills/             五個 agent 角色
    architect/          架構設計（Mode A 新建 / Mode B 差異擴充）
    pm-task-manager/    任務分解、狀態機、HITL 升級
    coder/              隔離分支上的實作與測試
    reviewer/           規則檢查、建置、測試、回歸驗證
    documenter/         事件記錄、最終文件、增量合併
  rules/              規則檔
    agentic-workflow.md   工作流核心規則（永遠套用）
    global-rules.md       全域行為規則（永遠套用）
    skill-preflight.md    所有 skill 的共用前置步驟
    shell-conventions.md  指令語法與 Windows 對照
    principles-*.md       規模原則（動態載入）
    lang-*.md             語言規範（動態載入）
    domain-*.md           領域規範（動態載入）
  config/
    database.example.json 測試資料庫連線範本（複製為 database.json 後填寫）
```

## 三個入口流程

| 流程 | 何時使用 |
|------|---------|
| `ai-dev-cycle` | 全新專案。Phase 0 收需求與架構草案 → Phase 7 產出文件 |
| `secondary-dev` | 已由本工作流完成的專案要加功能、改邏輯或還技術債 |
| `hotfix` | 某任務停在 `failed`，人工除錯後帶著指引重跑 |

## 開始使用

1. 若專案需要測試資料庫，複製連線範本並填入實際帳密（此檔已被 gitignore）：

```bash
cp .agents/config/database.example.json .agents/config/database.json
```

2. 在你的 AI Coding Agent 中執行 `ai-dev-cycle`，描述你要做的系統。
3. 在 **HITL-0a**（架構草案）與 **HITL-0b**（架構文件）兩個檢查點確認方向，
   之後流程會自動循環開發直到全部任務完成。

## 核心約定

- **HITL 檢查點不可跳過**：`HITL-0a` 規劃確認、`HITL-0b` 文件確認、`HITL-2` 失敗升級
- **Skill 之間不互相呼叫**：一律回報給 workflow，由 workflow 決定下一步
- **格式分工**：`architecture/` 用 Markdown + YAML frontmatter（規格），
  `artifacts/` 用 JSON（狀態）
- **機敏資訊**：密碼只存在 `.agents/config/database.json`；產出專案的
  `project_config.json` 一律使用 `${DB_PASSWORD}` 佔位符，且第一次 commit 前必須建立 `.gitignore`
- **品質關卡**：沒有自動化測試的任務不得標記完成；二次開發必須通過回歸驗證

完整規則見 [.agents/rules/agentic-workflow.md](.agents/rules/agentic-workflow.md)
與 [.agents/rules/global-rules.md](.agents/rules/global-rules.md)。
