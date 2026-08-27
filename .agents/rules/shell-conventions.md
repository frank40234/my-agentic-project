---
description: "工作流文件中指令的書寫與執行慣例，含 Windows / PowerShell 對照。"
alwaysApply: true
---

# 指令慣例（Shell Conventions）

## 書寫格式

所有 workflow 與 skill 文件中的指令，一律以 **POSIX shell** 表示。
執行前必須先判斷實際殼層，Windows PowerShell 需改寫為等效語法後再執行。

## PowerShell 5.1 對照表

| POSIX shell | PowerShell 5.1 等效寫法 |
|---|---|
| `mkdir -p {dir}` | `New-Item -ItemType Directory -Force {dir}` |
| `cmd1 && cmd2` | `cmd1; if ($?) { cmd2 }`（PS 5.1 無 `&&`） |
| `EXIT=$?` | `$EXIT = $LASTEXITCODE`（native exe 用 `$LASTEXITCODE`，非 `$?`） |
| `cmd 2>/dev/null` | `cmd 2>$null` |
| `cmd 2>&1` | **不要對 native exe 加 `2>&1`**：PS 5.1 會包成 NativeCommandError 並讓 `$?` 為 false。stderr 本來就會被捕捉 |
| `VAR=x cmd` | `$env:VAR = 'x'; cmd` |

## 執行規則

- 取得退出碼：POSIX 用 `$?`，PowerShell 對 native 執行檔一律用 `$LASTEXITCODE`
- 需要串接多個指令時，優先拆成多次呼叫，而非依賴殼層的串接語法
- 路徑一律使用 `project_config.json` 的 `project_root` 絕對路徑，不依賴當前工作目錄
