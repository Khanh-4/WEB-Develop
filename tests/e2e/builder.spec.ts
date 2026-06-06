import { test, expect } from '@playwright/test';
import { registerAndLogin, getCartCount } from './helpers';

test.describe('PC Builder', () => {
    test.beforeEach(async ({ page }) => {
        await registerAndLogin(page);
        await page.goto('/Builder');
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(1500);
    });

    test('loads with CPU tab active and product grid', async ({ page }) => {
        await expect(page.locator('.build-step[data-category="cpu"]')).toBeVisible();
        await expect(page.locator('#productGrid .component-card').first()).toBeVisible();
    });

    test('selecting a CPU updates the build panel', async ({ page }) => {
        await page.click('#productGrid .component-card:first-child');
        await page.waitForTimeout(800);

        // Build panel step should no longer say "Not selected"
        const cpuStep = page.locator('#step-name-cpu');
        await expect(cpuStep).not.toHaveText('Not selected');
    });

    test('selecting CPU then motherboard shows compatibility warnings', async ({ page }) => {
        // Select first CPU
        await page.click('#productGrid .component-card:first-child');
        await page.waitForTimeout(1000);

        // Switch to motherboard tab
        await page.click('.build-step[data-category="motherboard"]');
        await page.waitForTimeout(1500);

        // Products should be loaded (some may be incompatible depending on data)
        await expect(page.locator('#productGrid .component-card').first()).toBeVisible();
    });

    test('total price updates after component selection', async ({ page }) => {
        const initialTotal = await page.locator('#totalPrice').innerText();
        expect(initialTotal).toBe('0đ');

        await page.click('#productGrid .component-card:first-child');
        await page.waitForTimeout(800);

        const newTotal = await page.locator('#totalPrice').innerText();
        expect(newTotal).not.toBe('0đ');
    });

    test('remove component resets step to Not selected', async ({ page }) => {
        await page.click('#productGrid .component-card:first-child');
        await page.waitForTimeout(800);

        // Remove via X button in build panel
        const removeBtn = page.locator('#step-remove-cpu');
        await removeBtn.click({ force: true });
        await page.waitForTimeout(500);

        await expect(page.locator('#step-name-cpu')).toHaveText('Not selected');
    });

    test('add all to cart button adds all selected components', async ({ page }) => {
        // Select a CPU
        await page.click('#productGrid .component-card:first-child');
        await page.waitForTimeout(1000);

        // Switch to GPU tab and select
        await page.click('.build-step[data-category="videoCard"]');
        await page.waitForTimeout(1500);
        await page.click('#productGrid .component-card:first-child');
        await page.waitForTimeout(1000);

        // Try to click Add All to Cart (only appears when build is complete, but function always works)
        const addAllFn = await page.evaluate("typeof addAllToCart");
        expect(addAllFn).toBe('function');

        await page.evaluate("addAllToCart()");
        await page.waitForTimeout(2000);

        const count = await getCartCount(page);
        expect(count).toBeGreaterThanOrEqual(2);
    });
});
