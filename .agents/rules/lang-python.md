---
trigger: model_decision
description: "Python 語言慣用規範。涵蓋 PEP 8、型別提示、虛擬環境、測試框架等最佳實踐。"
---

# Python 語言慣用規範

## PEP 8 核心規則

- **snake_case**：函數、方法、變數、模組名稱
- **PascalCase**：類別名稱
- **UPPER_CASE**：常數（模組級別）
- **_single_leading_underscore**：內部使用（弱私有）
- **行寬上限 88 字元**：使用 Black formatter 的預設值
- **縮排使用 4 個空格**：禁止使用 Tab
- **空行規則**：頂層定義之間 2 行、類別方法之間 1 行

```python
MAX_RETRY_COUNT = 3  # 常數使用 UPPER_CASE

class UserService:
    """使用者服務，負責使用者相關的業務邏輯。"""

    def __init__(self, user_repo: UserRepository) -> None:
        self._user_repo = user_repo  # 私有屬性使用 _camelCase

    def get_active_users(self) -> list[User]:
        """取得所有啟用的使用者。"""
        return self._user_repo.find_by_status(is_active=True)
```

## Type Hints（型別標註）

- **所有公開函數必須有型別標註**：參數與回傳值都要標註
- **使用內建型別語法（Python 3.10+）**：`list[str]`、`dict[str, int]`、`str | None`
- **使用 `Optional` / `Union` / `TypeAlias`**：適用於 Python 3.9 以下
- **使用 `TypeVar` 和 `Generic`**：建立泛型類別與函數
- **使用 `Protocol`**：結構化子型別（鴨子型別的型別安全版本）
- **執行靜態檢查**：使用 mypy 或 pyright 驗證型別正確性

```python
from typing import Protocol

class Repository(Protocol):
    """儲存庫協議，定義資料存取的共用介面。"""

    async def find_by_id(self, entity_id: str) -> dict | None: ...
    async def save(self, entity: dict) -> None: ...

async def process_user(
    user_id: str,
    repo: Repository,
    *,
    notify: bool = False,
) -> dict | None:
    """處理使用者資料並回傳結果。

    Args:
        user_id: 使用者唯一識別碼。
        repo: 資料存取儲存庫。
        notify: 是否發送通知，預設為 False。

    Returns:
        處理後的使用者字典，若使用者不存在則回傳 None。
    """
    user = await repo.find_by_id(user_id)
    if user is None:
        return None
    # 處理邏輯...
    return user
```

## 虛擬環境與依賴管理

- **使用 `venv` 或 `poetry`**：每個專案獨立的虛擬環境
- **依賴檔案**：使用 `pyproject.toml`（優先）或 `requirements.txt`
- **鎖定版本**：`poetry.lock` 或 `pip freeze > requirements.lock`
- **分離開發依賴**：`[tool.poetry.group.dev.dependencies]` 或 `requirements-dev.txt`
- **不提交虛擬環境資料夾**：`.venv/` 加入 `.gitignore`
- **指定 Python 版本**：在 `pyproject.toml` 中標明 `requires-python`

```toml
# pyproject.toml 範例
[project]
name = "my-service"
version = "0.1.0"
requires-python = ">=3.11"
dependencies = [
    "fastapi>=0.100.0",
    "sqlalchemy>=2.0",
    "pydantic>=2.0",
]

[project.optional-dependencies]
dev = [
    "pytest>=7.0",
    "pytest-asyncio>=0.21",
    "mypy>=1.5",
    "ruff>=0.1.0",
]
```

## 測試

- **pytest 為預設測試框架**：不使用 unittest（除非既有專案）
- **fixtures 優先於 setUp/tearDown**：使用 `@pytest.fixture` 管理測試狀態
- **`conftest.py` 共用設定**：在目錄層級共用 fixtures 與 plugins
- **測試命名**：`test_<功能描述>` 或 `test_<方法名>_<情境>_<預期結果>`
- **使用 parametrize**：`@pytest.mark.parametrize` 減少重複測試程式碼
- **非同步測試**：使用 `pytest-asyncio` 搭配 `@pytest.mark.asyncio`
- **測試覆蓋率**：使用 `pytest-cov`，目標 ≥ 80%

```python
import pytest

@pytest.fixture
def user_service(mock_repo: MockUserRepository) -> UserService:
    """建立測試用的使用者服務實例。"""
    return UserService(user_repo=mock_repo)

class TestUserService:
    """使用者服務的單元測試。"""

    @pytest.mark.parametrize("user_id,expected", [
        ("valid-id", True),
        ("invalid-id", False),
        ("", False),
    ])
    def test_user_exists(
        self,
        user_service: UserService,
        user_id: str,
        expected: bool,
    ) -> None:
        """驗證使用者是否存在的檢查邏輯。"""
        result = user_service.exists(user_id)
        assert result == expected

    @pytest.mark.asyncio
    async def test_get_user_not_found_returns_none(
        self,
        user_service: UserService,
    ) -> None:
        """當使用者不存在時應回傳 None。"""
        result = await user_service.get_by_id("nonexistent")
        assert result is None
```

## 文件字串（Docstring）

- **Google Style docstring**：統一使用 Google 格式
- **模組級別 docstring**：每個 `.py` 檔案開頭說明模組用途
- **類別 docstring**：說明類別職責與使用方式
- **必要段落**：`Args`、`Returns`、`Raises`（視情況）

```python
"""使用者領域模型模組。

定義使用者相關的實體與值物件，作為業務邏輯的核心。
"""

class User:
    """使用者實體，封裝使用者的核心屬性與行為。

    Attributes:
        user_id: 使用者唯一識別碼。
        email: 電子郵件地址。
        is_active: 帳號是否啟用。

    Example:
        >>> user = User(user_id="u-001", email="test@example.com")
        >>> user.activate()
        >>> assert user.is_active
    """

    def activate(self) -> None:
        """啟用使用者帳號。

        Raises:
            ValueError: 當帳號已被停權時無法啟用。
        """
        if self._is_suspended:
            raise ValueError("已停權的帳號無法啟用")
        self.is_active = True
```

## Import 順序

- **順序**：標準庫 → 第三方套件 → 本地模組（每組之間空一行）
- **使用 isort 自動排序**：設定 `profile = "black"` 以相容 Black
- **避免萬用 import**：禁止 `from module import *`
- **相對 import**：僅限套件內部使用，外部一律使用絕對 import
- **延遲 import**：僅在解決循環依賴或加速啟動時使用

```python
# 標準庫
import asyncio
from datetime import datetime, timezone
from pathlib import Path

# 第三方套件
from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.ext.asyncio import AsyncSession

# 本地模組
from app.domain.user import User
from app.services.user_service import UserService
```

## 非同步程式設計

- **asyncio + aiohttp / FastAPI**：非同步 HTTP 框架與客戶端
- **避免混用 sync/async**：不在 async 函數中呼叫阻塞式 I/O
- **使用 `asyncio.gather()`**：平行執行多個非同步任務
- **使用 `asyncio.TaskGroup`（Python 3.11+）**：結構化並行，自動處理例外
- **資源管理**：使用 `async with` 管理非同步連線與 session

```python
async def fetch_user_data(user_ids: list[str]) -> list[UserData]:
    """批次取得使用者資料，平行執行以提升效能。"""
    async with aiohttp.ClientSession() as session:
        tasks = [_fetch_single(session, uid) for uid in user_ids]
        results = await asyncio.gather(*tasks, return_exceptions=True)

    # 過濾失敗的請求並記錄
    return [r for r in results if not isinstance(r, Exception)]
```

## 錯誤處理

- **自定義例外類別**：繼承自 `Exception`，建立領域專用例外
- **避免裸 `except`**：至少捕捉 `Exception`，不捕捉 `BaseException`
- **使用 `raise ... from ...`**：保留例外鏈
- **提前返回**：使用 guard clause 減少巢狀
- **記錄而非忽略**：捕捉的例外必須記錄或重新拋出

```python
class DomainError(Exception):
    """領域錯誤基底類別。"""

class UserNotFoundError(DomainError):
    """當找不到指定使用者時拋出。"""

    def __init__(self, user_id: str) -> None:
        self.user_id = user_id
        super().__init__(f"找不到使用者: {user_id}")

# 使用範例
try:
    user = await repo.find_by_id(user_id)
except DatabaseError as exc:
    logger.error("資料庫查詢失敗: %s", exc)
    raise UserNotFoundError(user_id) from exc
```