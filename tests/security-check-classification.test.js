const assert = require('assert');

const privateKey = /-----BEGIN PRIVATE KEY-----\s+[A-Za-z0-9+/]{16,}/i;
const smtpPassword = /SMTP_PASSWORD\s*[:=]\s*['"][^'"\s$<]{8,}/i;

assert.match('-----BEGIN PRIVATE KEY-----\nMIIEvQIBADANBgkqhkiG9w0BAQEF', privateKey);
assert.match('SMTP_PASSWORD="RealPassword!123"', smtpPassword);
assert.doesNotMatch('/-----BEGIN PRIVATE KEY-----/', privateKey);
assert.doesNotMatch('SMTP_PASSWORD=${SMTP_PASSWORD}', smtpPassword);
assert.doesNotMatch('Use -----BEGIN PRIVATE KEY----- followed by sanitized material.', privateKey);

console.log('security-check-classification: PASS');
