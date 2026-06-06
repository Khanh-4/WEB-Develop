import { Page } from '@playwright/test';

export function uniqueEmail() {
    // Combine timestamp + random to avoid collisions across parallel workers
    return `test${Date.now()}${Math.floor(Math.random() * 10000)}@e2e.local`;
}

export async function registerAndLogin(page: Page, email = uniqueEmail(), password = 'Test1234!') {
    await page.goto('/Account/Register');
    await page.fill('input[name="FullName"]', 'E2E User');
    await page.fill('input[name="Email"]', email);
    await page.fill('input[name="Password"]', password);
    await page.fill('input[name="ConfirmPassword"]', password);
    await page.click('button[type="submit"]');
    await page.waitForURL('/');
    return { email, password };
}

export async function addProductToCart(page: Page, index = 0) {
    await page.goto('/Products');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1200);
    await page.locator('button[title="Add to Cart"]').nth(index).click();
    await page.waitForTimeout(1000);
}

export async function getCartCount(page: Page): Promise<number> {
    const badge = page.locator('#cartCount');
    const visible = await badge.isVisible();
    if (!visible) return 0;
    return parseInt(await badge.innerText(), 10);
}
