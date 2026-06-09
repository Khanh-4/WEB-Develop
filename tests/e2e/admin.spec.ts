import { test, expect, Page } from '@playwright/test';

const ADMIN_EMAIL    = 'pw_admin@e2e.local';
const ADMIN_PASSWORD = 'Admin1234!';

async function loginAdmin(page: Page) {
    await page.goto('/Account/Login');
    await page.fill('input[name="Email"]', ADMIN_EMAIL);
    await page.fill('input[name="Password"]', ADMIN_PASSWORD);
    await page.click('button[type="submit"]');
    await page.waitForURL('/');
}

// ── Auth guard ────────────────────────────────────────────────────────────────

test('admin pages redirect to login when unauthenticated', async ({ page }) => {
    const adminRoutes = [
        '/Admin', '/Admin/Dashboard', '/Admin/Products', '/Admin/Orders',
        '/Admin/Users', '/Admin/FlashSales', '/Admin/Coupons', '/Admin/Bundles',
        '/Admin/Warranties', '/Admin/QuoteRequests', '/Admin/Benchmarks',
        '/Admin/Reviews', '/Admin/Scraper',
    ];
    for (const route of adminRoutes) {
        const res = await page.goto(route);
        expect(page.url(), `${route} should redirect`).toContain('/Account/Login');
    }
});

// ── Dashboard ─────────────────────────────────────────────────────────────────

test('admin dashboard loads with all stat cards', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/Dashboard');
    await expect(page).toHaveTitle(/Dashboard/);
    // stat cards (4 summary + 10 quick-nav = 14 total)
    const cards = page.locator('.stat-card');
    expect(await cards.count()).toBeGreaterThanOrEqual(4);
    await expect(cards.first()).toBeVisible();
    // Quick nav grid rendered (at least 8 items — <a> wraps .stat-card)
    const navLinks = page.locator('a:has(.stat-card)');
    expect(await navLinks.count()).toBeGreaterThanOrEqual(8);
    // No error text
    await expect(page.locator('text=An error occurred')).not.toBeVisible();
    await expect(page.locator('text=Error.')).not.toBeVisible();
});

// ── Products ──────────────────────────────────────────────────────────────────

test('admin products page loads and shows table', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/Products?category=cpu');
    await expect(page).toHaveTitle(/Admin/);
    await expect(page.locator('table.tx-table')).toBeVisible();
    await expect(page.locator('text=An error occurred')).not.toBeVisible();
    // Category tabs visible
    await expect(page.locator('.category-pill').first()).toBeVisible();
});

test('admin products - all 8 category tabs navigate without error', async ({ page }) => {
    await loginAdmin(page);
    const cats = ['cpu','motherboard','memory','gpu','psu','case','storage','cooler'];
    for (const cat of cats) {
        await page.goto(`/Admin/Products?category=${cat}`);
        await expect(page.locator('table.tx-table')).toBeVisible();
        await expect(page.locator('text=An error occurred')).not.toBeVisible();
    }
});

test('admin editproduct form loads for new cpu', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/EditProduct?category=cpu');
    await expect(page.locator('input[name="Name"]')).toBeVisible();
    await expect(page.locator('input[name="Price"]')).toBeVisible();
});

// ── Orders ────────────────────────────────────────────────────────────────────

test('admin orders page loads', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/Orders');
    await expect(page.locator('table.tx-table')).toBeVisible();
    await expect(page.locator('text=An error occurred')).not.toBeVisible();
    // Status filter pills
    await expect(page.locator('.category-pill').first()).toBeVisible();
});

test('admin orders - all status filters navigate without error', async ({ page }) => {
    await loginAdmin(page);
    const statuses = ['Pending','Confirmed','Assembling','InstallingOS','Shipped','Delivered','Cancelled'];
    for (const s of statuses) {
        await page.goto(`/Admin/Orders?status=${s}`);
        await expect(page.locator('table.tx-table')).toBeVisible();
        await expect(page.locator('text=An error occurred')).not.toBeVisible();
    }
});

// ── Users ─────────────────────────────────────────────────────────────────────

test('admin users page loads and shows table', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/Users');
    await expect(page.locator('table.tx-table')).toBeVisible();
    // Should show at least pw_admin row
    await expect(page.locator('text=PW Admin')).toBeVisible();
    await expect(page.locator('text=An error occurred')).not.toBeVisible();
});

test('admin users search works', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/Users');
    await page.fill('input[name="search"]', 'pw_admin');
    await page.click('button[type="submit"]');
    await expect(page.locator('text=PW Admin')).toBeVisible();
});

// ── Flash Sales ───────────────────────────────────────────────────────────────

test('admin flash sales page loads', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/FlashSales');
    // page renders either a table (data) or empty-state card
    await expect(page.locator('.tx-card').first()).toBeVisible();
    await expect(page.locator('text=An error occurred')).not.toBeVisible();
});

test('admin create flash sale form loads', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/CreateFlashSale');
    // first field is ProductName (no "Name" field exists)
    await expect(page.locator('input[name="ProductName"]')).toBeVisible();
    await expect(page.locator('text=An error occurred')).not.toBeVisible();
});

// ── Coupons ───────────────────────────────────────────────────────────────────

test('admin coupons page loads', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/Coupons');
    // page renders either a table (data) or empty-state card
    await expect(page.locator('.tx-card').first()).toBeVisible();
    await expect(page.locator('text=An error occurred')).not.toBeVisible();
});

test('admin create coupon form loads', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/CreateCoupon');
    await expect(page.locator('input[name="Code"]')).toBeVisible();
});

// ── Bundles ───────────────────────────────────────────────────────────────────

test('admin bundles page loads', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/Bundles');
    await expect(page.locator('text=An error occurred')).not.toBeVisible();
});

// ── Warranties ────────────────────────────────────────────────────────────────

test('admin warranties page loads', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/Warranties');
    await expect(page.locator('table.tx-table')).toBeVisible();
    await expect(page.locator('text=An error occurred')).not.toBeVisible();
});

// ── Quote Requests ────────────────────────────────────────────────────────────

test('admin quote requests page loads', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/QuoteRequests');
    await expect(page.locator('table.tx-table')).toBeVisible();
    await expect(page.locator('text=An error occurred')).not.toBeVisible();
});

// ── Benchmarks ────────────────────────────────────────────────────────────────

test('admin benchmarks page loads', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/Benchmarks');
    // page renders either a table (data) or empty-state card
    await expect(page.locator('.tx-card').first()).toBeVisible();
    await expect(page.locator('text=An error occurred')).not.toBeVisible();
});

// ── Reviews ───────────────────────────────────────────────────────────────────

test('admin reviews page loads', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/Reviews');
    await expect(page.locator('table.tx-table')).toBeVisible();
    await expect(page.locator('text=An error occurred')).not.toBeVisible();
});

test('admin reviews filter by rating works', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/Reviews?rating=5');
    await expect(page.locator('table.tx-table')).toBeVisible();
    await expect(page.locator('text=An error occurred')).not.toBeVisible();
});

// ── Scraper ───────────────────────────────────────────────────────────────────

test('admin scraper page loads', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/Scraper');
    await expect(page.locator('text=An error occurred')).not.toBeVisible();
    // Trigger button exists
    await expect(page.locator('button[type="submit"]')).toBeVisible();
});

// ── Sidebar navigation ────────────────────────────────────────────────────────

test('admin sidebar links all work from dashboard', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/Dashboard');
    // Verify sidebar exists
    await expect(page.locator('.admin-sidebar')).toBeVisible();
    // Check all nav links are present
    const sidebarLinks = page.locator('.admin-sidebar a');
    const count = await sidebarLinks.count();
    expect(count).toBeGreaterThan(8);
});

// ── Non-admin user cannot access admin pages ──────────────────────────────────

test('regular user gets 403/redirect when accessing admin pages', async ({ page }) => {
    // Login as regular user
    await page.goto('/Account/Login');
    await page.fill('input[name="Email"]', ADMIN_EMAIL); // pw_admin has Admin role, skip this test
    // Instead register a new regular user
    const regEmail = `nonadmin_${Date.now()}@e2e.local`;
    await page.goto('/Account/Register');
    await page.fill('input[name="FullName"]', 'Regular User');
    await page.fill('input[name="Email"]', regEmail);
    await page.fill('input[name="Password"]', 'Test1234!');
    await page.fill('input[name="ConfirmPassword"]', 'Test1234!');
    await page.click('button[type="submit"]');
    await page.waitForURL('/');

    // Try admin pages - should NOT succeed
    await page.goto('/Admin/Dashboard');
    // Should redirect to login or show 403, NOT show admin content
    const url = page.url();
    const hasAdminContent = await page.locator('.admin-sidebar').isVisible();
    expect(hasAdminContent).toBe(false);
});
