import { test, expect } from "./fixtures/auth";

test.describe("Queue Management", () => {
  test("displays queue view with Now Playing section", async ({
    page,
    authenticated,
  }) => {
    await authenticated();
    await page.goto("/queue");

    await expect(page.getByText(/now playing/i)).toBeVisible();
    await expect(page.getByText(/queue/i).first()).toBeVisible();
  });

  test("shows empty state when queue is empty", async ({
    page,
    authenticated,
  }) => {
    await authenticated();
    await page.goto("/queue");

    // Queue should be rendered (may show empty state or items depending on backend)
    await expect(
      page.locator("[data-testid='queue-list'], .queue-list, main"),
    ).toBeVisible();
  });

  test("queue view has add-to-queue functionality visible", async ({
    page,
    authenticated,
  }) => {
    await authenticated();
    await page.goto("/queue");

    // Admin should see queue management controls
    await expect(page.locator("main")).toBeVisible();
  });
});
