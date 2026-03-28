import { test, expect } from '@playwright/test';

test.describe('JobFlow API smoke', () => {
  test('swagger ui is reachable', async ({ request }) => {
    const response = await request.get('/swagger/index.html');
    expect(response.ok()).toBeTruthy();

    const html = await response.text();
    expect(html.toLowerCase()).toContain('swagger');
  });

  test('openapi document is served', async ({ request }) => {
    const response = await request.get('/swagger/v1/swagger.json');
    expect(response.ok()).toBeTruthy();

    const body = await response.json();
    expect(body).toHaveProperty('paths');
    expect(Object.keys(body.paths).length).toBeGreaterThan(0);
  });

  test('unknown route returns non-success status', async ({ request }) => {
    const response = await request.get('/api/this-route-should-not-exist');
    expect(response.status()).toBeGreaterThanOrEqual(400);
  });
});
