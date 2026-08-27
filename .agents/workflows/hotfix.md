---
description: >
  快速修復工作流。當任務因連續失敗而停在 failed 狀態、且人類已介入判斷除錯方向後，
  用於帶著人工指引重新啟動該任務的開發-測試迴圈。
---

# Hotfix 快速修復

> 適用時機：`/ai-dev-cycle` 或 `/secondary-dev` 的任務在 HITL-2 停下，
> 使用者離開流程自行除錯後，回來要求重跑該任務。

---

## Phase 1: 讀取狀態

1. 讀取 `artifacts/project_config.json` 取得 `project_root` 與 `active_rules`
2. 讀取 `{project_root}/artifacts/task_queue.json`，找出 `status` 為 `"failed"` 的任務

依結果分支：

| 情況 | 處理 |
|------|------|
| 恰好一個 `failed` 任務 | 直接採用 |
| 多個 `failed` 任務 | 列出清單（id / title / retry_count / last_error 摘要），請使用者選擇 |
| 沒有 `failed` 任務 | 告知使用者「目前沒有失敗中的任務」，並列出 `skipped` 任務供選擇；<br>若使用者選擇某個 `skipped` 任務，將其 `status` 改回 `failed` 後繼續 |

呈現該任務現況：

> 「任務 {id}「{title}」
> - 分支：{branch_name}
> - 重試次數：{retry_count} / {max_retries}
> - 最後錯誤：{last_error}
>
> 請提供除錯方向或修正指引：」

---

## Phase 2: 套用人工指引

1. 將使用者提供的除錯方向，以 `[Human guidance]: {內容}` 追加至該任務的 `last_error`
2. 呼叫 `pm-task-manager` skill：
   - `retry_count` 重置為 `0`
   - `status` 改為 `in_progress`
   - 立即寫回 `task_queue.json`

---

## Phase 3: 重新開發

呼叫 `coder` skill，等同 `/ai-dev-cycle` **Phase 4**（開發實作）。

- 分支：使用任務物件的 `branch_name`，但**前綴改為 `hotfix/`**
  （例如 `feature/task-007` → `hotfix/task-007`），由既有的 feature 分支切出：

```bash
cd {project_root}
git checkout {branch_name}
git checkout -b hotfix/task-{number}
```

- 提供給 coder 的上下文：任務物件、`related_architecture` 內容、
  含人工指引的 `last_error`、`regression_scope`（若有）
- 完成後 PM 將 `status` 更新為 `in_review`

---

## Phase 4: 測試驗證

呼叫 `reviewer` skill，等同 `/ai-dev-cycle` **Phase 5**。驗證對象為 hotfix 分支。

- **成功** → 進入 Phase 5
- **失敗且 `retry_count < max_retries`** → PM 遞增 `retry_count`、`status` 改回 `in_progress`，
  回到 Phase 3
- **失敗且 `retry_count >= max_retries`** → 任務維持 `failed`，觸發 HITL-2：
  > 「套用人工指引後仍連續失敗 {retry_count} 次。最後錯誤：{error_summary}
  > 請提供新的除錯方向，或輸入 **skip** 跳過此任務。」

  HITL-2 的完整處理規則見 [pm-task-manager](../skills/pm-task-manager/SKILL.md)。

---

## Phase 5: 合併與收尾

1. 合併 hotfix 分支回主線（主線分支名稱由 coder 偵測結果為準）：

```bash
cd {project_root}
git checkout {main_branch}
git merge --no-ff hotfix/task-{number} -m "merge(TASK-{id}): 合併 {任務標題}（hotfix）"
git branch -d hotfix/task-{number}
git branch -d {branch_name}
```

   合併衝突無法自動解決時，停止並觸發 HITL-2 請使用者介入。

2. PM 將 `status` 更新為 `done`，寫入 `completed_at`
3. 呼叫 `documenter`（Mode A）記錄事件
4. 回到原本中斷的流程：
   - 若 `task_queue.json` 仍有 `pending` 任務 → 告知使用者可繼續執行
     `/ai-dev-cycle`（Phase 3 派送）或 `/secondary-dev`
   - 若全部任務已完成 → 提醒使用者執行文件產出階段（Phase 7 / Phase 6）
