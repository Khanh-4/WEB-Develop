import { test, expect } from '@playwright/test';
import { registerAndLogin, addProductToCart, getCartCount } from './helpers';

test.describe('Products page', () => {
    test.beforeEach(async ({ page }) => {
        await registerAndLogin(page);
    });

    test('shows product grid with Add to Cart buttons', async ({ page }) => {
        await page.goto('/Products');
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(1200);

        await expect(page.locator('button:has(i.bi-cart-plus)').first()).toBeVisible();
    });

    test('add to cart shows success toast and increments badge', async ({ page }) => {
        await page.goto('/Products');
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(1200);

        await page.click('button:has(i.bi-cart-plus)');
        await expect(page.locator('.ts-toast')).toBeVisible({ timeout: 10000 });
        expect(await getCartCount(page)).toBe(1);
    });

    test('badge accumulates across multiple adds', async ({ page }) => {
        await page.goto('/Products');
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(1200);

        const btns = page.locator('button:has(i.bi-cart-plus)');
        await btns.nth(0).click();
        await page.waitForTimeout(800);
        await btns.nth(1).click();
        await page.waitForTimeout(800);
        await btns.nth(2).click();
        await page.waitForTimeout(1000);

        expect(await getCartCount(page)).toBe(3);
    });

    test('add to builder button navigates to builder with preselectection', async ({ page }) => {
        await page.goto('/Products');
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(1200);

        await page.click('button:has(i.bi-tools)');
        await expect(page).toHaveURL(/\/Builder\?preselect=/);
    });

    test('category filter narrows shown products', async ({ page }) => {
        await page.goto('/Products');
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(1200);

        await page.click('.category-pill[data-cat="cpu"]');
        await page.waitForTimeout(1200);

        const cpuCount = await page.locator('.product-card').count();
        expect(cpuCount).toBeGreaterThan(0);
    });
});

test.describe('Static pages', () => {
    test('About page loads', async ({ page }) => {
        await page.goto('/Home/About');
        await expect(page).toHaveTitle(/Giới thiệu/);
        await expect(page.locator('.tx-hero')).toBeVisible();
        await expect(page.locator('text=An error occurred')).not.toBeVisible();
    });

    test('Contact page loads and has a send button', async ({ page }) => {
        await page.goto('/Home/Contact');
        await expect(page).toHaveTitle(/Liên hệ/);
        await expect(page.locator('#contactBtn')).toBeVisible();
        await expect(page.locator('text=An error occurred')).not.toBeVisible();
    });

    test('Privacy page loads', async ({ page }) => {
        await page.goto('/Home/Privacy');
        await expect(page.locator('text=An error occurred')).not.toBeVisible();
    });
});
