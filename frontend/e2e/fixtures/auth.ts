import { test as base } from "@playwright/test";

/**
 * Auth fixtures for Playwright E2E tests.
 *
 * The dev server runs with VITE_AUTH_BYPASS=true, which auto-injects a
 * "Dev Admin" user with media-admin + media-user roles via setDevUser().
 *
 * These fixtures provide semantic helpers to make test intent clear.
 * For testing non-admin flows, we'd need to modify the auth store at runtime.
 */

interface AuthFixtures {
  /** Navigates and waits for auth-bypassed app to be ready (admin by default in dev) */
  authenticated: () => Promise<void>;
}

export const test = base.extend<AuthFixtures>({
  authenticated: async ({ page }, use) => {
    await use(async () => {
      await page.goto("/");
      await page.waitForLoadState("networkidle");
    });
  },
});

export { expect } from "@playwright/test";
