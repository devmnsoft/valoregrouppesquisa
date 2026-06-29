const { test, expect } = require('@playwright/test');

test('CSP live não bloqueia Bootstrap local nem chamada real de e-mail', async ({ page }) => {
  const publicUrl = process.env.VALORA_PUBLIC_URL || 'https://valoragroup.mnsoft.com.br';
  const cspErrors = [];
  const emailRequests = [];
  page.on('console', (msg) => {
    const text = msg.text();
    if (/Content Security Policy|violates .*src|blocked by CSP/i.test(text)) cspErrors.push(text);
  });
  page.on('request', (request) => {
    const url = request.url();
    if (/communications\/result\/.+\/send-email/.test(url)) emailRequests.push(url);
  });
  await page.goto(publicUrl, { waitUntil: 'domcontentloaded' });
  const cssLoaded = await page.evaluate(() => Array.from(document.styleSheets).some((s) => String(s.href || '').includes('vendor/bootstrap/bootstrap.min.css') || String(s.href || '').includes('bootstrap.min.css')));
  expect(cssLoaded).toBeTruthy();
  const bootstrapReady = await page.evaluate(() => !!window.bootstrap);
  expect(bootstrapReady).toBeTruthy();
  await page.evaluate(async () => {
    if (window.dispatchPostSurveyCommunication) {
      window.state = window.state || {};
      window.state.responses = window.state.responses || [];
      const response = window.state.responses.find((r) => r && r.id && r.resultToken && r.participant && r.participant.email);
      if (response) await window.dispatchPostSurveyCommunication(response.id);
    }
  });
  for (const url of emailRequests) {
    expect(url).toContain('https://api.valoragroup.mnsoft.com.br/communications/result/');
    expect(url).not.toContain('resp_demo');
  }
  expect(cspErrors.filter((e) => /bootstrap|communications\/result|api\.valoragroup/.test(e))).toEqual([]);
});
