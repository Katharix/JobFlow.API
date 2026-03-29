import { defineConfig } from '@playwright/test';

const baseURL = process.env.API_BASE_URL ?? 'https://localhost:7090';

export default defineConfig({
  testDir: './specs',
  fullyParallel: true,
  retries: process.env.CI ? 2 : 0,
  reporter: [['html', { open: 'never' }], ['list']],
  use: {
    baseURL,
    ignoreHTTPSErrors: true,
    extraHTTPHeaders: {
      Accept: 'application/json'
    }
  }
});
