---
description: "AI Coding Agentic 工作流核心規範。定義多 Agent 協作流程的行為準則。"
alwaysApply: true
---

# Agentic Workflow 核心規範

## 角色定義
本專案包含 5 個 Agent 角色：
1. **Architect** - 系統架構師，負責生成架構書
2. **PM** - 專案經理，負責任務拆解與調度
3. **Coder** - 開發者，負責撰寫程式碼
4. **Reviewer** - 審查者，負責沙盒測試與驗證
5. **Documenter** - 文件撰寫者，負責進度紀錄與最終文件

## 檔案約定
- 架構書：`artifacts/architecture.json`
- 任務佇列：`artifacts/task_queue.json`
- 進度日誌：`artifacts/progress_log.json`
- 最終狀態：`artifacts/system_state.json`
- 說明文件：`README.md`

## 防呆機制
- 同一任務連續失敗 3 次，必須停止並請求人類介入
- 每個 Subagent 啟動時只載入架構摘要與當前任務，禁止載入前一任務的對話紀錄