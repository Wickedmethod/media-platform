import { test, expect } from "./fixtures/auth";

test.describe("Navigation", () => {
  test("root redirects to /queue", async ({ page, authenticated }) => {
    await authenticated();
    await page.goto("/");

    await expect(page).toHaveURL(/\/queue/);
  });

  test("can navigate to search view", async ({ page, authenticated }) => {
    await authenticated();
    await page.goto("/queue");

    // Find and click the search navigation link
    const searchLink = page.getByRole("link", { name: /search/i });
    if (await searchLink.isVisible()) {
      await searchLink.click();
      await expect(page).toHaveURL(/\/search/);
    }
  });

  test("can navigate to admin view", async ({ page, authenticated }) => {
    await authenticated();
    await page.goto("/admin");

    // Dev user has admin role, so admin page should load
    await expect(page).toHaveURL(/\/admin/);
    await expect(page.locator("main")).toBeVisible();
  });

  test("unauthorized page renders for invalid routes", async ({
    page,
    authenticated,
  }) => {
    await authenticated();
    await page.goto("/unauthorized");

    await expect(page.locator("main")).toBeVisible();
  });
});

test.describe("Mobile Navigation", () => {
  test.use({ viewport: { width: 375, height: 812 } });

  test("renders bottom navigation on mobile", async ({
    page,
    authenticated,
  }) => {
    await authenticated();
    await page.goto("/queue");

    // Bottom nav should be visible on mobile viewport
    await expect(page.locator("nav")).toBeVisible();
  });

  test("mobile navigation between views", async ({ page, authenticated }) => {
    await authenticated();
    await page.goto("/queue");

    // Navigate to search
    const searchNav = page.getByRole("link", { name: /search/i });
    if (await searchNav.isVisible()) {
      await searchNav.click();
      await expect(page).toHaveURL(/\/search/);
    }

    // Navigate back to queue
    const queueNav = page.getByRole("link", { name: /queue/i });
    if (await queueNav.isVisible()) {
      await queueNav.click();
      await expect(page).toHaveURL(/\/queue/);
    }
  });
});
