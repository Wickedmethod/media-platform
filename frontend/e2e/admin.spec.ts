import { test, expect } from "./fixtures/auth";

test.describe("Admin Dashboard", () => {
  test("admin page is accessible for admin users", async ({
    page,
    authenticated,
  }) => {
    await authenticated();
    await page.goto("/admin");

    // Dev user has admin role — page should load
    await expect(page).toHaveURL(/\/admin/);
    await expect(page.locator("main")).toBeVisible();
  });

  test("admin page shows dashboard content", async ({
    page,
    authenticated,
  }) => {
    await authenticated();
    await page.goto("/admin");

    // Admin dashboard should have some content (kill switch, stats, etc.)
    await expect(page.locator("main")).toBeVisible();
  });

  test("admin flags page is accessible", async ({
    page,
    authenticated,
  }) => {
    await authenticated();
    await page.goto("/admin/flags");

    await expect(page).toHaveURL(/\/admin\/flags/);
    await expect(page.locator("main")).toBeVisible();
  });
});
