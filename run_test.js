const { chromium } = require('playwright');
(async () => {
  const browser = await chromium.launch();
  const context = await browser.newContext();
  const page = await context.newPage();
  await page.goto('http://localhost:5003/Account/Register');
  await page.fill('input[name="FullName"]', 'E2E User');
  await page.fill('input[name="Email"]', 'test12345@e2e.local');
  await page.fill('input[name="Password"]', 'Test1234!');
  await page.fill('input[name="ConfirmPassword"]', 'Test1234!');
  await page.click('button[type="submit"]');
  await page.waitForURL('http://localhost:5003/');
  
  await page.goto('http://localhost:5003/Products');
  await page.waitForTimeout(1000);
  await page.click('button:has(i.bi-cart-plus)');
  await page.waitForTimeout(1000);

  await page.goto('http://localhost:5003/Orders/Checkout');
  await page.fill('input[name="RecipientName"]', 'Nguyễn Văn E2E');
  await page.fill('input[name="Phone"]', '0912345678');
  await page.fill('textarea[name="ShippingAddress"]', '123 Đường Test, Q1, HCM');
  await page.click('button[type="submit"]');
  
  await page.waitForTimeout(2000);
  const errorText = await page.locator('#checkoutErrors').innerText();
  console.log("CHECKOUT ERRORS: " + errorText);
  
  await browser.close();
})();
