'use strict';

const sensitiveContentPatterns = Object.freeze([
  { name: 'BotToken', pattern: /\b\d{8,10}:[A-Za-z0-9_-]{30,}\b/ },
  { name: 'TELEGRAM_BOT_TOKEN', pattern: /TELEGRAM_BOT_TOKEN\s*[:=]\s*['\"]?[A-Za-z0-9:_-]{20,}/i },
  { name: 'SMTP_PASSWORD', pattern: /SMTP_PASSWORD\s*[:=]\s*['\"][^'\"\s$<]{8,}/i },
  { name: 'GOOGLE_APP_PASSWORD', pattern: /GOOGLE_APP_PASSWORD\s*[:=]\s*['\"][^'\"\s$<]{8,}/i },
  { name: 'serviceAccount', pattern: /serviceAccount\s*[:=]\s*['\"]?\{?/i },
  // A PEM header in documentation or detector source is harmless. Require
  // apparent key material after it before classifying the content as secret.
  { name: 'private_key', pattern: /-----BEGIN PRIVATE KEY-----(?:\\n|\s)+[A-Za-z0-9+/]{16,}|["']?private_key["']?\s*[:=]\s*["']-----BEGIN PRIVATE KEY-----[^"']*(?:\\n|\r?\n)?[A-Za-z0-9+/]{3,}/i },
]);

function contentForSecretScan(file, text) {
  if (!/\.[cm]?js$/i.test(file)) return text;
  // Detector implementations legitimately contain regex literals. Removing
  // only regex literals keeps quoted credentials in JavaScript detectable.
  return text.replace(/\/(?:\\.|[^/\r\n])+\/[dgimsuvy]*/g, '');
}

function classifySecrets(file, text) {
  const scanText = contentForSecretScan(file, text);
  return sensitiveContentPatterns
    .filter(({ pattern }) => pattern.test(scanText))
    .map(({ name }) => name);
}

module.exports = { classifySecrets, contentForSecretScan, sensitiveContentPatterns };
