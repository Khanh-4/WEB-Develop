import { test, expect } from '@playwright/test';
import { uniqueEmail } from './helpers';

test.describe('Auth', () => {
    test('register redirects to home and shows username in navbar', async ({ page }) => {
        const email = uniqueEmail();
        await page.goto('/Account/Register');
        await page.fill('input[name="FullName"]', 'Test User');
        await page.fill('input[name="Email"]', email);
        await page.fill('input[name="Password"]', 'Test1234!');
        await page.fill('input[name="ConfirmPassword"]', 'Test1234!');
        await page.click('button[type="submit"]');

        await expect(page).toHaveURL('/');
        // tx-header is the Mock V2 header container
        await expect(page.locator('.tx-header')).toContainText('Test User');
    });

    test('login with wrong password shows error', async ({ page }) => {
        await page.goto('/Account/Login');
        await page.fill('input[name="Email"]', 'nobody@nowhere.com');
        await page.fill('input[name="Password"]', 'WrongPass1!');
        await page.click('button[type="submit"]');

        await expect(page.locator('.validation-summary-errors').first()).toBeVisible();
    });

    test('logout clears session and cart badge', async ({ page }) => {
        const email = uniqueEmail();
        await page.goto('/Account/Register');
        await page.fill('input[name="FullName"]', 'Test User');
        await page.fill('input[name="Email"]', email);
        await page.fill('input[name="Password"]', 'Test1234!');
        await page.fill('input[name="ConfirmPassword"]', 'Test1234!');
        await page.click('button[type="submit"]');
        await page.waitForURL('/');

        // Open user dropdown via the avatar button
        await page.click('button[data-bs-toggle="dropdown"] .avatar');
        // Click the logout form's submit button
        await page.click('form[action*="/Account/Logout"] button');
        await page.waitForURL('/');

        await expect(page.locator('#cartCount')).not.toBeVisible();
        await expect(page.locator('.tx-header')).not.toContainText('Test User');
    });

    test('protected routes redirect to login when unauthenticated', async ({ page }) => {
        for (const route of ['/Cart', '/Orders', '/Orders/Checkout']) {
            await page.goto(route);
            await expect(page).toHaveURL(/Account\/Login/);
        }
    });
});
