const { test, expect } = require('@playwright/test');
const fs = require('fs');

test('legacy email flow uses real response id and token', async () => {
  const app = fs.readFileSync('app.js', 'utf8');
  expect(app).toContain('dispatchPostSurveyCommunication(response.id)');
  expect(app).toContain('responseId:response.id');
  expect(app).toContain('resultToken:response.resultToken');
  expect(app).toContain('/communications/result/${encodeURIComponent(responseId)}/send-email');
  expect(app).not.toContain(['resp','demo'].join('_'));
  expect(app).not.toContain(['demo','response'].join('_'));
});
