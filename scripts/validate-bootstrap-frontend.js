#!/usr/bin/env node
'use strict';
const fs = require('fs');
function fail(message){ console.error(`validate-bootstrap-frontend: FAIL - ${message}`); process.exit(1); }
function read(file){ return fs.existsSync(file) ? fs.readFileSync(file,'utf8') : fail(`${file} ausente`); }
const index = read('index.html');
const app = read('app.js');
const pkg = read('package.json');
if (!/bootstrap(?:\.min)?\.css/i.test(index)) fail('Bootstrap CSS não carregado no index.html');
if (!/bootstrap\.bundle(?:\.min)?\.js/i.test(index)) fail('Bootstrap JS bundle não carregado no index.html');
const scripts = [...index.matchAll(/<script[^>]+src=["']([^"']+)/gi)].map(m => m[1].split('/').pop().split('?')[0]);
const order = Object.fromEntries(['api-client.js','api-repository.js','gateway-client.js','repository.js','app.js'].map(name => [name, scripts.indexOf(name)]));
if (Object.values(order).some(pos => pos < 0)) fail('ordem de scripts obrigatórios incompleta');
if (!(order['api-client.js'] < order['api-repository.js'] && order['api-repository.js'] < order['repository.js'] && order['repository.js'] < order['app.js'] && order['gateway-client.js'] < order['app.js'])) fail('ordem de api-client/api-repository/gateway/repository/app inválida');
const combined = `${index}\n${app}\n${pkg}`;
for (const fw of ['react','angular','vue']) {
  if (new RegExp(`\\b${fw}\\b|${fw}\\.`, 'i').test(combined)) fail(`frontend não deve usar ${fw}`);
}
for (const cls of ['container','card','section']) {
  if (!new RegExp(`class=["'][^"']*${cls}`).test(combined)) fail(`classe/componente esperado ausente: ${cls}`);
}
console.log('validate-bootstrap-frontend: PASS');
