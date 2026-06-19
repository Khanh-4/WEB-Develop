import { test, expect, Page } from '@playwright/test';
import { registerAndLogin, addProductToCart, uniqueEmail } from './helpers';

const ADMIN_EMAIL    = 'pw_admin@e2e.local';
const ADMIN_PASSWORD = 'Admin1234!';

async function loginAdmin(page: Page) {
    await page.goto('/Account/Login');
    await page.fill('input[name="Email"]', ADMIN_EMAIL);
    await page.fill('input[name="Password"]', ADMIN_PASSWORD);
    await page.click('button[type="submit"]');
    await page.waitForURL('/');
}

test.describe('TechSpecs Fixes & Features Verification', () => {

    // 1. Forgot password link in Development mode (must register user first)
    test('1. forgot password dev link is displayed', async ({ page }) => {
        // Register a user first to guarantee they exist in the DB
        const email = uniqueEmail();
        await registerAndLogin(page, email, 'Test1234!');
        
        // Log out to clear current session so we can request reset
        await page.goto('/Account/Login'); // Going to login usually clears or handles it, but let's clear cookies to be sure
        await page.context().clearCookies();
        
        // Now perform the forgot password flow
        await page.goto('/Account/ForgotPassword');
        await page.fill('input[name="Email"]', email);
        await page.click('button[type="submit"]');
        
        await page.waitForURL('/Account/ForgotPasswordConfirmation');
        
        // Assert warning box is visible (signifying Development mode) and has the resetLink
        const devAlert = page.locator('.alert-warning');
        await expect(devAlert).toBeVisible();
        await expect(devAlert).toContainText('[Development Mode]');
        
        const link = devAlert.locator('a');
        await expect(link).toBeVisible();
        const href = await link.getAttribute('href');
        expect(href).toContain('/Account/ResetPassword');
        expect(href).toContain('token=');
    });

    // 2. Light Theme - Contrast and color verification
    test('2. light theme color contrast overrides', async ({ page }) => {
        await page.goto('/');
        
        // Inject light theme directly
        await page.evaluate(() => {
            document.documentElement.setAttribute('data-theme', 'light');
        });
        
        // Wait briefly for style changes to propagate
        await page.waitForTimeout(500);

        // Flash sale price element should be red in light theme
        const flashSalePrice = page.locator('.flash-sale-price').first();
        if (await flashSalePrice.count() > 0) {
            const color = await flashSalePrice.evaluate((el) => window.getComputedStyle(el).color);
            // Red color hex is #dc2626 which evaluates to rgb(220, 38, 38)
            expect(color).toBe('rgb(220, 38, 38)');
        }

        // Glass text white buttons/links in light theme should have dark text color
        const glassTextWhite = page.locator('.btn.glass.text-white, a.btn.glass-sm.text-white').first();
        if (await glassTextWhite.count() > 0) {
            const textColor = await glassTextWhite.evaluate((el) => window.getComputedStyle(el).color);
            expect(textColor).not.toBe('rgb(255, 255, 255)');
        }
    });

    // 3. Mini Cart Drawer - Chatbot & Live Chat Overlay Hiding (must login first)
    test('3. cart drawer hides chatbot & live chat widgets', async ({ page }) => {
        await registerAndLogin(page);
        await page.goto('/');
        await page.waitForLoadState('networkidle');
        
        const chatToggle = page.locator('#chatToggle');
        const liveChat = page.locator('#liveChatWidget');

        // Initially both widgets should be visible
        await expect(chatToggle).toBeVisible();
        await expect(liveChat).toBeVisible();

        // Click the cart badge/button to open quick cart drawer (ID is cartOpen)
        await page.click('#cartOpen');
        await page.waitForSelector('#cartDrawer.open');

        // Check they are hidden (display: none or not visible)
        await expect(chatToggle).not.toBeVisible();
        await expect(liveChat).not.toBeVisible();

        // Close drawer (ID is cartClose)
        await page.click('#cartClose');
        await page.waitForSelector('#cartDrawer:not(.open)');

        // They should be visible again
        await expect(chatToggle).toBeVisible();
        await expect(liveChat).toBeVisible();
    });

    // 4. Cart Recalculate on Delete
    test('4. subtotal recalculates automatically upon deletion', async ({ page }) => {
        await registerAndLogin(page);
        
        // Add 2 products to cart
        await addProductToCart(page, 0);
        await addProductToCart(page, 1);

        await page.goto('/Cart');
        await page.waitForLoadState('networkidle');

        const initialTotalText = await page.locator('#cartTotal').innerText();
        const initialTotal = parseInt(initialTotalText.replace(/[^0-9]/g, ''), 10);
        
        const deleteButtons = page.locator('button[onclick^="removeItem"]');
        const initialCount = await deleteButtons.count();

        // Click the first delete button
        await page.click('button[onclick^="removeItem"]');
        
        // Wait for delete button count to decrease, indicating the page has reloaded
        await expect(deleteButtons).toHaveCount(initialCount - 1);
        
        const newTotalText = await page.locator('#cartTotal').innerText();
        const newTotal = parseInt(newTotalText.replace(/[^0-9]/g, ''), 10);

        expect(newTotal).toBeLessThan(initialTotal);
    });

    // 5. Multi-tab Drawer Sync
    test('5. cart drawer syncs across tabs on focus', async ({ context, page: page1 }) => {
        await registerAndLogin(page1);
        await page1.goto('/');
        
        // Open the drawer on page1 (ID is cartOpen)
        await page1.click('#cartOpen');
        await page1.waitForSelector('#cartDrawer.open');
        
        const page2 = await context.newPage();
        await page2.goto('/Products');
        
        // Add a product on page2
        await addProductToCart(page2, 0);
        
        // Now return to page1 (bring to front and focus)
        await page1.bringToFront();
        await page1.evaluate(() => window.dispatchEvent(new Event('focus')));
        
        // Wait for cart sync call to complete
        await page1.waitForTimeout(2000);
        
        // Verify drawer now has items (selector is #cartDrawerItems .cart-line)
        const drawerItems = page1.locator('#cartDrawerItems .cart-line');
        await expect(drawerItems.first()).toBeVisible();
    });

    // 6. Admin Consolidated Promotions
    test('6. admin promotions consolidated page works', async ({ page }) => {
        await loginAdmin(page);

        // Access the promotions dashboard
        await page.goto('/Admin/Promotions');
        await expect(page).toHaveURL('/Admin/Promotions');
        
        // Verify we have tabs for Flash Sales, Coupons, and Bundles
        const tabs = page.locator('.nav-tabs .nav-link');
        await expect(tabs).toHaveCount(3);
        await expect(tabs.nth(0)).toContainText('Flash Sales');
        await expect(tabs.nth(1)).toContainText('Coupons');
        await expect(tabs.nth(2)).toContainText('Combo / Bundles');

        // Check sidebar has the single consolidated Khuyến mãi link
        const promoSidebarLink = page.locator('.admin-sidebar a[href="/Admin/Promotions"]');
        await expect(promoSidebarLink).toBeVisible();

        // Check navigation redirect works for old endpoints
        await page.goto('/Admin/FlashSales');
        await expect(page).toHaveURL('/Admin/Promotions'); // should redirect to Promotions

        // Access create page
        await page.goto('/Admin/CreatePromotion');
        await expect(page.locator('#promotionTypeSelect')).toBeVisible();

        // Select Coupon form
        await page.selectOption('#promotionTypeSelect', 'Coupon');
        await expect(page.locator('#couponFormSection')).toBeVisible();
        await expect(page.locator('#flashSaleFormSection')).not.toBeVisible();
    });
});
