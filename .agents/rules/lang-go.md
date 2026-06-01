---
name: lang-go
description: "Go 語言慣用規範。涵蓋 gofmt、錯誤處理、並行處理、專案結構等最佳實踐。"
alwaysApply: false
---

# Go 語言慣用規範

## 格式化

- **gofmt / goimports 強制執行**：所有程式碼提交前必須通過 `gofmt` 格式化
- **使用 goimports**：自動管理 import 區塊的排序與增刪
- **編輯器整合**：設定儲存時自動執行 `goimports`
- **CI 檢查**：在 CI pipeline 中加入 `gofmt -l` 驗證

```bash
# 格式化所有 Go 檔案
gofmt -w .

# 使用 goimports（包含 import 排序）
goimports -w .

# CI 中檢查格式化（有未格式化的檔案則失敗）
test -z "$(gofmt -l .)"
```

## 命名慣例

- **Exported（公開）使用 PascalCase**：`UserService`、`NewRepository`、`MaxRetries`
- **Unexported（私有）使用 camelCase**：`userService`、`newRepository`、`maxRetries`
- **短變數名慣例**：`i`（迴圈索引）、`err`（錯誤）、`ctx`（context）、`req`/`resp`（請求/回應）
- **介面命名**：單方法介面加 `-er` 後綴（`Reader`、`Writer`、`Stringer`）
- **縮寫一致性**：全大寫（`HTTPClient`、`UserID`）而非混合大小寫
- **Package 命名**：小寫單字、不用底線或混合大小寫、簡短且有意義
- **避免 stuttering**：`user.User` 而非 `user.UserStruct`

```go
// ✅ 正確的命名
package user

type Service struct {
    repo Repository
    log  *slog.Logger
}

func NewService(repo Repository, log *slog.Logger) *Service {
    return &Service{repo: repo, log: log}
}

func (s *Service) FindByID(ctx context.Context, id string) (*User, error) {
    // ...
}

// ❌ 錯誤 - stuttering
// package user
// type UserService struct { ... }  // user.UserService 重複了
```

## 錯誤處理

- **`error` 作為最後回傳值**：多回傳值時 error 放最後
- **立即檢查錯誤**：`if err != nil` 緊接在可能出錯的呼叫後
- **使用 `errors.Is` / `errors.As` 比較**：不使用 `==` 比較 error
- **自定義 error 實作 `Error()` 方法**：實作 `error` 介面
- **使用 `fmt.Errorf` 包裝**：`fmt.Errorf("讀取使用者失敗: %w", err)` 保留錯誤鏈
- **不使用 panic**：僅限初始化階段的不可恢復錯誤（如設定檔缺失）
- **Sentinel errors**：使用 `var ErrNotFound = errors.New(...)` 定義

```go
// 定義 Sentinel errors
var (
    ErrNotFound   = errors.New("找不到資源")
    ErrConflict   = errors.New("資源衝突")
    ErrValidation = errors.New("驗證失敗")
)

// 自定義 error 型別
type ValidationError struct {
    Field   string
    Message string
}

func (e *ValidationError) Error() string {
    return fmt.Sprintf("欄位 %s 驗證失敗: %s", e.Field, e.Message)
}

func (e *ValidationError) Unwrap() error {
    return ErrValidation
}

// 使用範例
func (s *Service) GetUser(ctx context.Context, id string) (*User, error) {
    user, err := s.repo.FindByID(ctx, id)
    if err != nil {
        return nil, fmt.Errorf("取得使用者 %s 失敗: %w", id, err)
    }
    if user == nil {
        return nil, fmt.Errorf("使用者 %s: %w", id, ErrNotFound)
    }
    return user, nil
}

// 呼叫端使用 errors.Is 比較
user, err := svc.GetUser(ctx, userID)
if err != nil {
    if errors.Is(err, ErrNotFound) {
        // 處理找不到的情況
    }
    return err
}
```

## 並行處理

- **goroutine + channel**：Go 的核心並行模型
- **`context.Context` 傳遞超時與取消**：所有跨邊界的呼叫都傳遞 context
- **`sync.WaitGroup`**：等待一組 goroutine 完成
- **`sync.Mutex` / `sync.RWMutex`**：保護共享狀態
- **`errgroup.Group`**：平行執行並收集第一個錯誤
- **避免 goroutine 洩漏**：確保每個 goroutine 都有退出條件
- **Channel 方向標註**：`chan<-`（只寫）、`<-chan`（只讀）

```go
import "golang.org/x/sync/errgroup"

// 使用 errgroup 平行處理並收集錯誤
func (s *Service) FetchAll(ctx context.Context, ids []string) ([]*User, error) {
    g, ctx := errgroup.WithContext(ctx)
    users := make([]*User, len(ids))

    for i, id := range ids {
        i, id := i, id // 捕獲迴圈變數（Go 1.22 前需要）
        g.Go(func() error {
            user, err := s.repo.FindByID(ctx, id)
            if err != nil {
                return fmt.Errorf("取得使用者 %s 失敗: %w", id, err)
            }
            users[i] = user
            return nil
        })
    }

    if err := g.Wait(); err != nil {
        return nil, err
    }
    return users, nil
}

// 使用 context 控制超時
func (s *Service) ProcessWithTimeout(ctx context.Context) error {
    ctx, cancel := context.WithTimeout(ctx, 30*time.Second)
    defer cancel()

    return s.longRunningTask(ctx)
}
```

## 介面

- **小介面優先（1-3 個方法）**：遵循介面隔離原則
- **消費端定義介面**：在使用介面的套件中定義，而非提供方
- **不預先定義介面**：先寫具體型別，有需要時再抽取介面
- **隱式實作**：Go 的介面是隱式滿足的，不需要 `implements` 關鍵字
- **介面組合**：用小介面組合出大介面

```go
// ✅ 在消費端定義小介面
// 檔案: service/user.go
package service

// UserFinder 定義查詢使用者的介面（由消費端定義）
type UserFinder interface {
    FindByID(ctx context.Context, id string) (*domain.User, error)
}

// UserSaver 定義儲存使用者的介面
type UserSaver interface {
    Save(ctx context.Context, user *domain.User) error
}

// UserRepository 組合多個小介面
type UserRepository interface {
    UserFinder
    UserSaver
}

type UserService struct {
    repo UserRepository
}

// ❌ 錯誤 - 在提供方預先定義大介面
// package repository
// type Repository interface {
//     FindByID(...)
//     Save(...)
//     Delete(...)
//     List(...)
//     Count(...)
//     ... 20 個方法
// }
```

## 專案結構

- **遵循標準佈局**：依功能分層組織目錄
- **`cmd/`**：應用程式進入點，每個子目錄對應一個可執行檔
- **`internal/`**：私有套件，外部無法 import
- **`pkg/`**：可被外部專案使用的公開套件（謹慎使用）
- **避免過早抽象**：先平鋪再重構，不預先建立空目錄

```
project-root/
├── cmd/
│   ├── api-server/          # API 伺服器進入點
│   │   └── main.go
│   └── worker/              # 背景工作者進入點
│       └── main.go
├── internal/
│   ├── domain/              # 領域模型（實體、值物件）
│   │   ├── user.go
│   │   └── order.go
│   ├── service/             # 業務邏輯層
│   │   └── user_service.go
│   ├── repository/          # 資料存取層
│   │   ├── postgres/
│   │   │   └── user_repo.go
│   │   └── redis/
│   │       └── cache.go
│   ├── handler/             # HTTP 處理常式
│   │   └── user_handler.go
│   └── config/              # 設定載入與驗證
│       └── config.go
├── pkg/                     # 公開套件（可選）
│   └── httputil/
├── migrations/              # 資料庫 migration
├── go.mod
├── go.sum
├── Makefile
└── README.md
```

## 依賴管理

- **使用 `go mod`**：官方模組管理工具
- **不提交 `vendor/`**：除非有離線建置等特殊需求
- **定期更新依賴**：`go get -u ./...` 並測試相容性
- **使用 `go mod tidy`**：清理未使用的依賴
- **版本選擇**：遵循語義化版本，major 版本變更需更改 import 路徑

```bash
# 初始化模組
go mod init github.com/org/project

# 新增依賴
go get github.com/gin-gonic/gin@latest

# 清理未使用的依賴
go mod tidy

# 驗證依賴完整性
go mod verify
```

## 測試

- **`_test.go` 慣例**：測試檔案與被測檔案放在同一目錄
- **Table-Driven Tests**：使用表格驅動測試減少重複
- **testify 斷言庫**：使用 `assert` 和 `require` 套件
- **測試命名**：`Test<函數名>_<情境>` 或 `Test<函數名>/<子測試>`
- **使用 `t.Helper()`**：在測試輔助函數中標記
- **使用 `t.Parallel()`**：獨立的測試案例啟用平行執行
- **Mock 使用 interface**：透過介面注入替換依賴

```go
func TestUserService_GetByID(t *testing.T) {
    tests := []struct {
        name     string
        userID   string
        mockUser *domain.User
        mockErr  error
        wantErr  error
    }{
        {
            name:     "成功取得使用者",
            userID:   "u-001",
            mockUser: &domain.User{ID: "u-001", Name: "測試使用者"},
            mockErr:  nil,
            wantErr:  nil,
        },
        {
            name:     "使用者不存在",
            userID:   "u-999",
            mockUser: nil,
            mockErr:  nil,
            wantErr:  ErrNotFound,
        },
        {
            name:     "資料庫錯誤",
            userID:   "u-001",
            mockUser: nil,
            mockErr:  errors.New("連線逾時"),
            wantErr:  errors.New("連線逾時"),
        },
    }

    for _, tt := range tests {
        tt := tt // 捕獲迴圈變數
        t.Run(tt.name, func(t *testing.T) {
            t.Parallel()

            repo := &mockRepo{
                user: tt.mockUser,
                err:  tt.mockErr,
            }
            svc := NewService(repo, slog.Default())

            got, err := svc.GetByID(context.Background(), tt.userID)

            if tt.wantErr != nil {
                assert.Error(t, err)
                assert.Nil(t, got)
                return
            }

            require.NoError(t, err)
            assert.Equal(t, tt.mockUser, got)
        })
    }
}
```
