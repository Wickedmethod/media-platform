# MEDIA-715: E2E Testing with Playwright

## Story

**Epic:** MEDIA-FE-ADMIN — Admin Frontend  
**Priority:** Medium  
**Effort:** 3 points  
**Status:** ⏳ Planned  
**Depends on:** MEDIA-700 (project setup), MEDIA-701 (auth), MEDIA-702 (queue view)

---

## Summary

Set up Playwright for end-to-end testing of the Vue SPA. Cover critical user flows: login → search → add to queue → view queue → admin controls. Uses MSW for API mocking — no real backend needed for tests.

---

## Setup

```typescript
// playwright.config.ts
import { defineConfig, devices } from '@playwright/test'

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: 'html',
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
    { name: 'mobile', use: { ...devices['Pixel 7'] } },
  ],
  webServer: {
    command: 'pnpm dev',
    url: 'http://localhost:5173',
    reuseExistingServer: !process.env.CI,
  },
})
```

---

## Auth Mocking

Keycloak auth is mocked by injecting a fake token state:

```typescript
// e2e/fixtures/auth.ts
import { test as base } from '@playwright/test'

interface AuthFixtures {
  asUser: () => Promise<void>
  asAdmin: () => Promise<void>
}

export const test = base.extend<AuthFixtures>({
  asUser: async ({ page }, use) => {
    await use(async () => {
      // Inject mock auth state into Pinia store
      await page.evaluate(() => {
        const fakeToken = { sub: 'user-1', preferred_username: 'testuser', realm_access: { roles: ['media-user'] } }
        localStorage.setItem('auth-state', JSON.stringify({
          isAuthenticated: true,
          user: fakeToken,
          token: 'fake-jwt-token',
        }))
      })
      await page.reload()
    })
  },
  asAdmin: async ({ page }, use) => {
    await use(async () => {
      await page.evaluate(() => {
        const fakeToken = { sub: 'admin-1', preferred_username: 'admin', realm_access: { roles: ['media-admin'] } }
        localStorage.setItem('auth-state', JSON.stringify({
          isAuthenticated: true,
          user: fakeToken,
          token: 'fake-admin-token',
        }))
      })
      await page.reload()
    })
  },
})
```

---

## Critical Test Flows

### 1. Queue Management Flow

```typescript
// e2e/queue.spec.ts
test.describe('Queue Management', () => {
  test('user can search and add song to queue', async ({ page, asUser }) => {
    await asUser()
    await page.goto('/search')

    await page.getByPlaceholder('Search YouTube').fill('bohemian rhapsody')
    await page.waitForSelector('[data-testid="search-result"]')

    await page.getByTestId('search-result').first().getByRole('button', { name: 'Add' }).click()
    await expect(page.getByText('Added to queue')).toBeVisible()

    await page.goto('/queue')
    await expect(page.getByText('Bohemian Rhapsody')).toBeVisible()
  })

  test('user can only delete own queue items', async ({ page, asUser }) => {
    await asUser()
    await page.goto('/queue')

    // Own item — delete visible
    const ownItem = page.getByTestId('queue-item').filter({ hasText: 'testuser' })
    await expect(ownItem.getByRole('button', { name: 'Remove' })).toBeVisible()

    // Other's item — no delete button
    const otherItem = page.getByTestId('queue-item').filter({ hasText: 'otheruser' })
    await expect(otherItem.getByRole('button', { name: 'Remove' })).not.toBeVisible()
  })
})
```

### 2. Admin Dashboard Flow

```typescript
// e2e/admin.spec.ts
test.describe('Admin Dashboard', () => {
  test('admin can toggle kill switch', async ({ page, asAdmin }) => {
    await asAdmin()
    await page.goto('/admin')

    const killSwitch = page.getByTestId('kill-switch-toggle')
    await killSwitch.click()

    await expect(page.getByText('Kill switch activated')).toBeVisible()
  })

  test('non-admin cannot access admin route', async ({ page, asUser }) => {
    await asUser()
    await page.goto('/admin')
    await expect(page).toHaveURL('/queue') // Redirected
  })
})
```

### 3. Mobile Navigation Flow

```typescript
// e2e/navigation.spec.ts
test.describe('Mobile Navigation', () => {
  test.use({ viewport: { width: 375, height: 812 } }) // iPhone

  test('bottom tab bar navigates between views', async ({ page, asUser }) => {
    await asUser()
    await page.goto('/queue')

    await expect(page.getByTestId('bottom-tab-bar')).toBeVisible()

    await page.getByRole('link', { name: 'Search' }).click()
    await expect(page).toHaveURL('/search')

    await page.getByRole('link', { name: 'Queue' }).click()
    await expect(page).toHaveURL('/queue')
  })
})
```

### 4. Real-Time Updates (SSE)

```typescript
// e2e/realtime.spec.ts
test('queue updates in real-time via SSE', async ({ page, asUser }) => {
  await asUser()
  await page.goto('/queue')

  // Simulate SSE event by mocking EventSource
  await page.evaluate(() => {
    window.dispatchEvent(new CustomEvent('test-sse', {
      detail: { type: 'queue-updated', data: {} }
    }))
  })

  // Queue should refetch and show updated items
  await expect(page.getByTestId('queue-item')).toHaveCount(3)
})
```

---

## File Structure

```
e2e/
├── fixtures/
│   ├── auth.ts          ← Auth mock helpers
│   └── api-mocks.ts     ← MSW route handlers
├── queue.spec.ts         ← Queue flow tests
├── admin.spec.ts         ← Admin dashboard tests
├── navigation.spec.ts    ← Mobile/desktop nav tests
├── search.spec.ts        ← YouTube search tests
├── realtime.spec.ts      ← SSE real-time tests
└── auth.spec.ts          ← Login/logout flow tests
```

---

## Package.json Scripts

```json
{
  "scripts": {
    "test:e2e": "playwright test",
    "test:e2e:ui": "playwright test --ui",
    "test:e2e:mobile": "playwright test --project=mobile"
  }
}
```

---

## Tasks

- [ ] Install Playwright (`pnpm create playwright`)
- [ ] Configure `playwright.config.ts` with Chrome + mobile projects
- [ ] Create auth mock fixtures (user + admin)
- [ ] Create API mock setup (MSW or route intercepts)
- [ ] Write queue management flow test
- [ ] Write search → add to queue flow test
- [ ] Write admin dashboard flow test
- [ ] Write mobile navigation test
- [ ] Write auth redirect test (unauthorized → login)
- [ ] Add `test:e2e` script to `package.json`
- [ ] Add `data-testid` attributes to key components

---

## Acceptance Criteria

- [ ] `pnpm test:e2e` runs all tests in headless Chrome
- [ ] Tests pass on both desktop and mobile viewports
- [ ] Auth is mocked (no real Keycloak needed)
- [ ] API is mocked (no real backend needed)
- [ ] Critical flow covered: search → add → queue → play
- [ ] Admin access control tested (redirect on unauthorized)
- [ ] Screenshots captured on failure
- [ ] Tests run in < 60 seconds total
