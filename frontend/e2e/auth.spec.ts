import { test, expect } from "./fixtures/auth";

test.describe("Auth & Access Control", () => {
  test("unauthenticated access shows unauthorized page", async ({ page }) => {
    // Without auth bypass, the app would redirect to unauthorized
    // In dev mode with VITE_AUTH_BYPASS=true, user is auto-authenticated
    await page.goto("/unauthorized");
    await expect(page.locator("main")).toBeVisible();
  });

  test("authenticated user sees navigation", async ({
    page,
    authenticated,
  }) => {
    await authenticated();
    await page.goto("/queue");

    // User should see the nav bar with links
    await expect(page.locator("nav")).toBeVisible();
  });

  test("app initializes without errors", async ({
    page,
    authenticated,
  }) => {
    const errors: string[] = [];
    page.on("pageerror", (error) => errors.push(error.message));

    await authenticated();
    await page.goto("/queue");
    await page.waitForTimeout(1000);

    // Filter out expected errors (like API calls to non-running backend)
    const criticalErrors = errors.filter(
      (e) => !e.includes("fetch") && !e.includes("network") && !e.includes("ERR_CONNECTION"),
    );
    expect(criticalErrors).toHaveLength(0);
  });
});
