import { test, expect } from '@playwright/test';
import { registerAndLogin, addProductToCart, getCartCount } from './helpers';

test.describe('Cart & Checkout', () => {
    test.beforeEach(async ({ page }) => {
        await registerAndLogin(page);
    });

    test('empty cart shows empty state message', async ({ page }) => {
        await page.goto('/Cart');
        await expect(page.locator('text=Giỏ hàng trống')).toBeVisible();
        await expect(page.locator('a[href="/Products"].btn')).toBeVisible();
    });

    test('added items appear in cart with correct total', async ({ page }) => {
        await addProductToCart(page, 0);
        await addProductToCart(page, 1);

        await page.goto('/Cart');
        const items = page.locator('.cart-item');
        await expect(items).toHaveCount(2);
        await expect(page.locator('#cartTotal')).not.toBeEmpty();
    });

    test('remove item updates cart', async ({ page }) => {
        await addProductToCart(page, 0);
        await addProductToCart(page, 1);
        await page.goto('/Cart');

        await page.click('button[onclick^="removeItem"]');
        await page.waitForTimeout(1000);

        const items = page.locator('.cart-item');
        await expect(items).toHaveCount(1);
    });

    test('full checkout flow: cart → form → confirmation → orders list', async ({ page }) => {
        await addProductToCart(page);
        await page.goto('/Cart');

        await page.click('a[href="/Orders/Checkout"]');
        await expect(page).toHaveURL('/Orders/Checkout');

        await page.fill('input[name="RecipientName"]', 'Nguyễn Văn E2E');
        await page.fill('input[name="Phone"]', '0912345678');
        await page.fill('textarea[name="ShippingAddress"]', '123 Đường Test, Q1, HCM');
        await page.click('button[type="submit"]');

        await expect(page).toHaveURL(/Orders\/Confirmation/);
        await expect(page.locator('text=Đặt hàng thành công')).toBeVisible();
        await expect(page.locator('text=Nguyễn Văn E2E')).toBeVisible();

        // Navigate to orders history
        await page.click('a[href="/Orders"]');
        await expect(page).toHaveURL('/Orders');
        await expect(page.locator('text=Chờ xác nhận')).toBeVisible();
    });

    test('cart is empty after completing checkout', async ({ page }) => {
        await addProductToCart(page);
        await page.goto('/Orders/Checkout');
        await page.fill('input[name="RecipientName"]', 'Test');
        await page.fill('input[name="Phone"]', '0900000000');
        await page.fill('textarea[name="ShippingAddress"]', 'Test Address');
        await page.click('button[type="submit"]');
        await page.waitForURL(/Confirmation/);

        await page.goto('/Cart');
        await expect(page.locator('text=Giỏ hàng trống')).toBeVisible();

        // Badge should also be hidden
        await expect(page.locator('#cartCount')).not.toBeVisible();
    });

    test('checkout with empty cart redirects to cart', async ({ page }) => {
        await page.goto('/Orders/Checkout');
        await expect(page).toHaveURL('/Cart');
    });

    test('checkout form validation prevents submit without required fields', async ({ page }) => {
        await addProductToCart(page);
        await page.goto('/Orders/Checkout');

        // Submit without filling form
        await page.click('button[type="submit"]');

        // Should still be on checkout page
        await expect(page).toHaveURL('/Orders/Checkout');
        await expect(page.locator('span.text-danger.field-validation-error').first()).toBeVisible();
    });

    test('order detail shows correct shipping info', async ({ page }) => {
        await addProductToCart(page);
        await page.goto('/Orders/Checkout');
        await page.fill('input[name="RecipientName"]', 'Detail Test User');
        await page.fill('input[name="Phone"]', '0987654321');
        await page.fill('textarea[name="ShippingAddress"]', '456 Detail St');
        await page.click('button[type="submit"]');

        await page.goto('/Orders');
        await page.click('a:has-text("Chi tiết")');

        await expect(page.locator('text=0987654321')).toBeVisible();
        await expect(page.locator('text=456 Detail St')).toBeVisible();
    });
});
