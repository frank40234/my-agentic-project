---
trigger: model_decision
description: "TypeScript 語言慣用規範。涵蓋嚴格模式、型別系統、ESLint、React/Node.js 慣例。"
---

# TypeScript 語言慣用規範

## Strict Mode

- **啟用 `strict: true`**：在 `tsconfig.json` 中啟用所有嚴格檢查
- **關鍵嚴格選項**：`noImplicitAny`、`strictNullChecks`、`strictFunctionTypes`
- **不允許 `@ts-ignore`**：如必要使用 `@ts-expect-error` 並附註原因
- **啟用 `noUncheckedIndexedAccess`**：索引存取回傳 `T | undefined`

```jsonc
// tsconfig.json
{
  "compilerOptions": {
    "strict": true,
    "noUncheckedIndexedAccess": true,
    "noImplicitReturns": true,
    "noFallthroughCasesInSwitch": true,
    "forceConsistentCasingInFileNames": true,
    "esModuleInterop": true,
    "moduleResolution": "bundler",
    "target": "ES2022"
  }
}
```

## 型別系統

- **`interface` 優先於 `type`**：用於物件結構定義（可擴展、可合併）
- **`type` 用於聯合、交叉、工具型別**：`type Result = Success | Failure`
- **使用 `const enum`**：減少執行時開銷（或使用 `as const` 物件替代）
- **禁止 `any`**：使用 `unknown` 替代，搭配型別守衛收窄
- **使用 Discriminated Union**：取代多層繼承，實現型別安全的多型
- **善用 Utility Types**：`Partial<T>`、`Pick<T, K>`、`Omit<T, K>`、`Record<K, V>`

```typescript
// ✅ 使用 interface 定義物件結構
interface User {
  readonly id: string;
  name: string;
  email: string;
  role: UserRole;
}

// ✅ 使用 type 定義聯合型別
type UserRole = 'admin' | 'editor' | 'viewer';

// ✅ Discriminated Union 處理不同狀態
type ApiResult<T> =
  | { status: 'success'; data: T }
  | { status: 'error'; error: string; code: number };

// ✅ 型別守衛收窄 unknown
function isUser(value: unknown): value is User {
  return (
    typeof value === 'object' &&
    value !== null &&
    'id' in value &&
    'email' in value
  );
}
```

## 命名慣例

- **camelCase**：變數、函數、方法、參數
- **PascalCase**：類別、介面、型別別名、列舉、React 元件
- **kebab-case**：檔案名稱（如 `user-service.ts`、`use-auth.ts`）
- **UPPER_CASE**：常數（模組級別、不可變）
- **布林變數**：使用 `is`、`has`、`should`、`can` 前綴
- **事件處理**：`on` 前綴（如 `onClick`、`onSubmit`）

```typescript
// 檔案名稱: user-service.ts
const MAX_RETRY_COUNT = 3;

interface UserService {
  isAuthenticated: boolean;
  hasPermission(role: UserRole): boolean;
  onUserCreated?: (user: User) => void;
}
```

## ESLint + Prettier

- **統一程式碼風格**：ESLint 負責邏輯檢查，Prettier 負責格式化
- **使用 `@typescript-eslint`**：TypeScript 專用的 ESLint 規則
- **禁用 `console.log`（生產環境）**：使用 `no-console` 規則，改用 logger
- **啟用 `no-unused-vars`**：使用 `@typescript-eslint/no-unused-vars`
- **排序 import**：使用 `eslint-plugin-import` 自動排序

```jsonc
// .eslintrc.json 核心設定
{
  "extends": [
    "eslint:recommended",
    "plugin:@typescript-eslint/recommended-type-checked",
    "prettier"
  ],
  "rules": {
    "no-console": "warn",
    "@typescript-eslint/no-unused-vars": ["error", { "argsIgnorePattern": "^_" }],
    "@typescript-eslint/no-explicit-any": "error",
    "@typescript-eslint/explicit-function-return-type": "warn"
  }
}
```

## Import 規範

- **使用路徑別名（`@/`）**：在 `tsconfig.json` 中設定 `paths`，避免深層相對路徑
- **避免 barrel export 過度使用**：大型專案中 `index.ts` 會影響 tree-shaking 與編譯速度
- **Import 順序**：外部套件 → 內部模組 → 型別 import
- **使用 `import type`**：純型別 import 使用 `import type { ... }`

```typescript
// ✅ 正確的 import 順序與風格
import { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';

import { userApi } from '@/api/user-api';
import { UserCard } from '@/components/user-card';
import { useAuth } from '@/hooks/use-auth';

import type { User, UserRole } from '@/types/user';
```

## 非同步處理

- **async/await 優先**：取代 `.then()` 鏈式呼叫
- **`Promise.all` 平行處理**：獨立的非同步操作同時執行
- **`Promise.allSettled`**：需要所有結果（含失敗）時使用
- **避免回呼地獄**：層層巢狀的 callback 重構為 async/await
- **錯誤處理**：使用 try/catch 或 Result 型別

```typescript
// ✅ 平行處理獨立的非同步操作
async function loadDashboard(userId: string): Promise<DashboardData> {
  const [user, orders, notifications] = await Promise.all([
    fetchUser(userId),
    fetchOrders(userId),
    fetchNotifications(userId),
  ]);

  return { user, orders, notifications };
}

// ✅ Result 型別處理可預期的錯誤
type Result<T, E = Error> =
  | { ok: true; value: T }
  | { ok: false; error: E };

async function safeParseJson<T>(raw: string): Promise<Result<T>> {
  try {
    const data = JSON.parse(raw) as T;
    return { ok: true, value: data };
  } catch (error) {
    return { ok: false, error: error as Error };
  }
}
```

## React 專用規範

- **函數元件 + Hooks**：不使用 class component
- **Props 使用 interface 定義**：明確標註每個 prop 的型別
- **避免 prop drilling**：使用 Context 或狀態管理庫（Zustand / Jotai）
- **Custom Hooks 抽取邏輯**：以 `use` 開頭命名，封裝可重用的狀態邏輯
- **Memo 策略**：`React.memo`、`useMemo`、`useCallback` 僅在效能瓶頸時使用
- **事件處理命名**：`handle` 前綴（如 `handleSubmit`）

```tsx
// 檔案名稱: user-profile.tsx

interface UserProfileProps {
  /** 使用者唯一識別碼 */
  userId: string;
  /** 編輯完成時的回呼 */
  onEditComplete?: (user: User) => void;
}

export function UserProfile({ userId, onEditComplete }: UserProfileProps) {
  const { data: user, isLoading } = useUser(userId);

  const handleSave = async (formData: UserFormData) => {
    const updated = await updateUser(userId, formData);
    onEditComplete?.(updated);
  };

  if (isLoading) return <Skeleton />;
  if (!user) return <NotFound />;

  return <UserForm user={user} onSubmit={handleSave} />;
}
```

## Node.js 專用規範

- **Express / Fastify middleware 慣例**：錯誤處理 middleware 放在最後
- **環境變數**：使用 `dotenv` 載入，透過 Zod schema 驗證
- **不信任外部輸入**：所有 request body/params/query 必須驗證（Zod / class-validator）
- **結構化日誌**：使用 pino 或 winston，JSON 格式輸出
- **優雅關閉**：處理 `SIGTERM` / `SIGINT` 信號，清理連線

```typescript
// 環境變數驗證
import { z } from 'zod';

const envSchema = z.object({
  NODE_ENV: z.enum(['development', 'production', 'test']),
  PORT: z.coerce.number().default(3000),
  DATABASE_URL: z.string().url(),
});

export const env = envSchema.parse(process.env);
```

## 測試

- **Jest / Vitest**：擇一作為測試框架（Vitest 在 Vite 專案中優先）
- **`describe` / `it` 語法**：結構化組織測試案例
- **Mock 使用 `jest.mock` / `vi.mock`**：隔離外部依賴
- **測試命名**：`it('should <預期行為> when <條件>')`
- **AAA 模式**：Arrange → Act → Assert
- **避免測試實作細節**：測試行為而非內部狀態

```typescript
describe('UserService', () => {
  let service: UserService;
  let mockRepo: MockedObject<UserRepository>;

  beforeEach(() => {
    mockRepo = createMockRepo();
    service = new UserService(mockRepo);
  });

  it('should return user when valid id is provided', async () => {
    // Arrange
    const expectedUser = createTestUser({ id: 'u-001' });
    mockRepo.findById.mockResolvedValue(expectedUser);

    // Act
    const result = await service.getById('u-001');

    // Assert
    expect(result).toEqual(expectedUser);
    expect(mockRepo.findById).toHaveBeenCalledWith('u-001');
  });

  it('should throw NotFoundError when user does not exist', async () => {
    mockRepo.findById.mockResolvedValue(null);

    await expect(service.getById('invalid')).rejects.toThrow(NotFoundError);
  });
});
```