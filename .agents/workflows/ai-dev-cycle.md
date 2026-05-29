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
3. 以**純文字摘要**呈現規劃草案（模組清單、技術決策、預計文件清單）
4. **不寫入任何檔案**

### Step 0.3: 人工確認草案（HITL-0a）
**停止**。Architect 已呈現草案，等待使用者確認：
> 「以上為架構規劃草案，請確認。輸入 **approve** 開始產生架構文件，或提供修改意見。」

- 若核准 → 進入 Step 0.4
- 若提供意見 → Architect 修改草案後重新呈現

### Step 0.4: 產生架構文件
Architect 依核准的草案寫入所有架構文件（JSON + Markdown）。
- Small：產生單一 `architecture.json` + `architecture.md`
- Medium：產生 L0 + 各模組 L1（JSON + Markdown）
- Large：產生 L0 + 各模組 L1 + 各模組所有 L2（JSON + Markdown），**同一模組的 L2 一次全部產出**

### Step 0.5: 人工確認文件（HITL-0b）
**停止**。呈現已產生的文件清單摘要：
> 「架構文件已產生完畢，請確認。輸入 **approve** 繼續開發，或提供修改意見。」

- 若核准 → 進入 Phase 1（Medium/Large）或 Phase 2（Small）
- 若提供意見 → Architect 修改相關文件後重新呈現

---

## Phase 1: 模組層級確認（僅 Medium / Large 專案）

Small 專案直接跳至 Phase 2。

### Step 1.1: 選取下一個模組
讀取 L0 主架構。找到下一個符合條件的模組：
- status 為 `"pending"`
- 所有依賴模組的 status 為 `"done"`

### Step 1.2: Large 專案 — L2 批次確認（HITL-1）
**僅 Large 專案**，且尚未對此模組進行 L2 確認時執行。

**停止**。呈現該模組所有 L2 子模組的清單摘要：
> 「模組 {module_name} 的子模組設計如下，請確認。輸入 **approve** 開始此模組的任務分解，或提供修改意見。」

- 若核准 → 進入 Phase 2
- 若提供意見 → Architect 修改對應 L2 文件後重新呈現

Medium 專案：此步驟直接略過，進入 Phase 2。

---

## Phase 2: 任務分解

### Step 2.1: 建立任務佇列
呼叫 `pm-task-manager` skill，將當前範圍分解為開發任務：
- Small：將單一架構拆分成任務
- Medium/Large：將當前模組的 L1（與 L2）拆分成任務
輸出至 `artifacts/task_queue.json`，並向使用者展示任務清單。

### Step 2.2: 派送第一個任務
取出第一個 pending 任務，進入 Phase 3。

---

## Phase 3: 開發實作

### Step 3.1: 建立 Branch
呼叫 `coder` skill。Coder 為當前任務建立隔離的 Git branch。

### Step 3.2: 實作
Coder 依任務描述與相關架構實作功能。

**記憶體管理原則**：只提供 Coder：
- 與此任務相關的架構摘要
- 當前任務細節
- 若為重試，提供錯誤日誌

**不得**提供其他任務的上下文或不相關模組的架構。

### Step 3.3: Commit
Coder 以描述性訊息 commit 變更。

---

## Phase 4: 測試驗證

### Step 4.1: 建置與測試
呼叫 `reviewer` skill。Reviewer 從技術棧自動判斷正確的建置 / 測試指令並執行。

### Step 4.2: 結果判斷

**成功（exit_code == 0）**：
進入 Phase 5。

**失敗（exit_code != 0）且 retry_count < 3**：
PM 分析錯誤，產生修正指示，將任務送回 Coder。
返回 Phase 3 Step 3.2。

**失敗且 retry_count >= 3（HITL-2）**：
**停止**。告知使用者：
> 「任務 {id}「{title}」已連續失敗 {retry_count} 次。
> 最後錯誤：{error_summary}
> 請提供除錯指引，或輸入 **skip** 跳過此任務。」

- 若使用者提供指引：重置 retry_count，套用指引，返回 Phase 3
- 若使用者輸入 `skip`：標記任務為 skipped，進入 Phase 5

---

## Phase 5: 進度更新與循環

### Step 5.1: 更新任務狀態
PM 將當前任務標記為 `"done"`（或 `"skipped"`）於 `task_queue.json`。

### Step 5.2: 更新進度日誌
呼叫 `documenter` skill（Mode A）更新 `artifacts/progress_log.json`。
此為非同步背景操作，不需等待完成。

### Step 5.3: 判斷下一步
檢查剩餘項目：

- **當前模組 / 專案還有更多任務？**
  → 返回 Phase 2 Step 2.2 派送下一個任務

- **當前模組完成，還有更多模組？（Medium/Large）**
  → 在 L0 中將模組狀態更新為 `"done"`
  → 返回 Phase 1 Step 1.1 選取下一個模組

- **全部完成？**
  → 進入 Phase 6

---

## Phase 6: 文件產出與完成

### Step 6.1: 產生最終文件
呼叫 `documenter` skill（Mode B）：
1. 掃描最終程式碼庫
2. 產生 `README.md`
3. 產生 `artifacts/system_state.json`（含未完成項目、技術債、擴充點）
4. 產生 `architecture/ARCHITECTURE-INDEX.md`（medium/large）

### Step 6.2: 通知完成
告知使用者：「專案開發完成！」
列出所有產生的文件，並標示任何被跳過的任務或已知問題。
