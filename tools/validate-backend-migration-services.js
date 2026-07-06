#!/usr/bin/env node
const fs = require('fs');
const path = require('path');

const root = path.resolve(__dirname, '..');
const servicePath = path.join(root, 'backend/Valora.Application/Services/Migration/LegacyMigrationServices.cs');
const source = fs.readFileSync(servicePath, 'utf8');
const errors = [];

function fail(message) {
  errors.push(message);
}

if (source.includes('SerializeToElement(e.ValueKind switch')) {
  fail('LegacyMigrationServices.cs still contains the CS0411-prone SerializeToElement(e.ValueKind switch pattern.');
}

const maskMethodMatch = source.match(/private\s+JsonElement\s+Mask\s*\(\s*JsonElement\s+element\s*\)[\s\S]*?return\s+JsonSerializer\.SerializeToElement<object\?>\(maskedValue\);[\s\S]*?\n    \}/);
if (!maskMethodMatch) {
  fail('Mask(JsonElement element) must use JsonSerializer.SerializeToElement<object?>(maskedValue).');
}

if (!/object\?\s+maskedValue\s*=\s*element\.ValueKind\s+switch/.test(source)) {
  fail('Mask(JsonElement element) must store the switch result in explicit object? maskedValue.');
}

const sensitivePattern = /password\|senha\|token\|secret\|smtp\|connection\|string\|hash\|refresh/i;
if (!sensitivePattern.test(source)) {
  fail('Sensitive field matcher must cover password, senha, token, secret, smtp, connection, string, hash and refresh.');
}

const longSingleLineTypes = source
  .split(/\r?\n/)
  .map((line, index) => ({ line, number: index + 1 }))
  .filter(({ line }) => /public\s+(sealed\s+)?(class|record|interface)\b/.test(line) && line.length > 180);
if (longSingleLineTypes.length > 0) {
  fail(`Found likely single-line type declarations above 180 chars: ${longSingleLineTypes.map(x => x.number).join(', ')}.`);
}

const longLines = source
  .split(/\r?\n/)
  .map((line, index) => ({ line, number: index + 1 }))
  .filter(({ line }) => line.length > 240);
if (longLines.length > 0) {
  fail(`Found lines above 240 chars, suggesting compacted methods/classes: ${longLines.map(x => x.number).join(', ')}.`);
}

const leakPatterns = [
  /request\.PayloadJson[^)]*AuditEntry/,
  /InputRawJson/,
  /NormalizedRawJson/,
  /ExpectedRawJson/,
  /ActualRawJson/,
  /password\s*=\s*[^"']*JsonSerializer\.Serialize/i,
  /token\s*=\s*[^"']*JsonSerializer\.Serialize/i,
  /secret\s*=\s*[^"']*JsonSerializer\.Serialize/i,
  /smtp\s*=\s*[^"']*JsonSerializer\.Serialize/i,
];
for (const pattern of leakPatterns) {
  if (pattern.test(source)) {
    fail(`Potential unmasked migration payload exposure matched pattern: ${pattern}.`);
  }
}

if (!source.includes('"***MASKED***"')) {
  fail('Masking marker ***MASKED*** not found.');
}

if (errors.length > 0) {
  console.error('Backend migration services validation failed:');
  for (const error of errors) {
    console.error(`- ${error}`);
  }
  process.exit(1);
}

console.log('Backend migration services validation passed.');
