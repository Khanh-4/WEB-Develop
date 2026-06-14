import { test, expect } from '@playwright/test';

test.describe('Homepage', () => {
    test('hero renders with spotlight container', async ({ page }) => {
        await page.goto('/');
        await page.waitForSelector('.tx-hero', { timeout: 10_000 });
        await expect(page.locator('.hero-spotlight')).toBeVisible();
    });

    test('Build PC ngay CTA links to /Builder', async ({ page }) => {
        await page.goto('/');
        await page.waitForSelector('.tx-hero', { timeout: 10_000 });
        const cta = page.locator('.tx-hero a[href="/Builder"]').first();
        await expect(cta).toBeVisible();
        await expect(cta).toContainText('Build PC ngay');
    });

    test('Flash Sale section renders on homepage', async ({ page }) => {
        await page.goto('/');
        // FlashSale ViewComponent must render — confirms it's included in the view
        await expect(page.locator('#flashsale')).toBeAttached({ timeout: 15_000 });
    });
});
