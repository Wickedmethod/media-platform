import { test, expect } from "./fixtures/auth";

test.describe("Search", () => {
  test("search page renders with search input", async ({
    page,
    authenticated,
  }) => {
    await authenticated();
    await page.goto("/search");

    // Search input should be visible
    const searchInput = page.getByPlaceholder(/search/i);
    await expect(searchInput).toBeVisible();
  });

  test("can type in search input", async ({ page, authenticated }) => {
    await authenticated();
    await page.goto("/search");

    const searchInput = page.getByPlaceholder(/search/i);
    await searchInput.fill("bohemian rhapsody");
    await expect(searchInput).toHaveValue("bohemian rhapsody");
  });
});
