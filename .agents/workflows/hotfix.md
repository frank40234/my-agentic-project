---
description: >
  快速修復工作流。當人類介入除錯後，用於重新啟動特定任務的開發-測試迴圈。
---

# Hotfix 快速修復

## Step 1：讀取狀態
讀取 `artifacts/task_queue.json`，找到 `status` 為 `"failed"` 的任務。

## Step 2：套用人類指導
將使用者提供的除錯方向整合到任務上下文中。

## Step 3：重新執行
重置 `retry_count` 為 0，從階段 3（開發）重新開始該任務。