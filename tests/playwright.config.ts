import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
    testDir: './e2e',
    timeout: 30_000,
    retries: 1,
    reporter: [['list'], ['html', { outputFolder: 'playwright-report', open: 'never' }]],

    use: {
        baseURL: 'http://localhost:5003',
        trace: 'on-first-retry',
        screenshot: 'only-on-failure',
    },

    projects: [
        { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
    ],

    // App is expected to already be running (dotnet run --launch-profile http)
    // webServer: { command: '...', url: 'http://localhost:5003', reuseExistingServer: true }
});
