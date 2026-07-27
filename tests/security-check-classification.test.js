const assert = require('assert');
const { classifySecrets } = require('../scripts/security-secret-classifier');

// Build deliberately unsafe fixtures in memory so the repository scanner can
// still detect an accidentally committed literal credential in this test file.
assert.deepStrictEqual(classifySecrets('fixture.txt', ['-----BEGIN PRIVATE ', 'KEY-----\n', 'MIIEvQIBADANBgkqhkiG9w0BAQEF'].join('')), ['private_key']);
assert.deepStrictEqual(classifySecrets('fixture.txt', ['SMTP_', 'PASSWORD="', 'RealPassword!123"'].join('')), ['SMTP_PASSWORD']);
assert.deepStrictEqual(classifySecrets('fixture.txt', ['GOOGLE_APP_', 'PASSWORD="', 'RealGooglePassword!123"'].join('')), ['GOOGLE_APP_PASSWORD']);
assert.deepStrictEqual(classifySecrets('fixture.txt', ['private_', 'key="-----BEGIN PRIVATE KEY-----abc123"'].join('')), ['private_key']);

for (const safe of [
  'process.env.SMTP_PASSWORD',
  '${SMTP_PASSWORD}',
  '<SMTP_PASSWORD>',
  'SMTP_PASSWORD_EXAMPLE',
  'Use -----BEGIN PRIVATE KEY----- followed by sanitized material.',
]) assert.deepStrictEqual(classifySecrets('fixture.txt', safe), []);

assert.deepStrictEqual(classifySecrets('validator.js', 'const detector = /SMTP_PASSWORD\\s*=/i;'), []);

console.log('security-check-classification: PASS');
