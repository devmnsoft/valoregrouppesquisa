const fs = require('fs');
const path = require('path');
const root = process.cwd();
const webJs = path.join(root, 'backend/Valora.Web/wwwroot/js');
function fail(message) { console.error(message); process.exit(1); }
function read(relative) { return fs.readFileSync(path.join(root, relative), 'utf8'); }
function exists(relative) { return fs.existsSync(path.join(root, relative)); }
function walk(dir) {
  if (!fs.existsSync(dir)) return [];
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap(entry => {
    const full = path.join(dir, entry.name);
    return entry.isDirectory() ? walk(full) : [full];
  });
}
const requiredApiFiles = [
  'auth-api.js', 'plans-api.js', 'public-survey-api.js', 'results-api.js',
  'certificates-api.js', 'communications-api.js', 'health-api.js'
];
for (const file of requiredApiFiles) {
  const relative = `backend/Valora.Web/wwwroot/js/api/${file}`;
  if (!exists(relative)) fail(`missing ${relative}`);
  const content = read(relative);
  if (!/AjaxClient\.(get|post|put|patch|delete|requestJson|requestBinary)/.test(content)) {
    fail(`${relative} does not call AjaxClient`);
  }
}
const ajax = read('backend/Valora.Web/wwwroot/js/api/ajax-client.js');
['$.ajax', 'requestJson', 'requestBinary', 'setToken', 'getToken', 'clearToken', 'normalizeApiError', 'generateCorrelationId', 'X-Correlation-Id'].forEach(token => {
  if (!ajax.includes(token)) fail(`ajax-client missing ${token}`);
});
const pages = walk(path.join(webJs, 'pages')).filter(file => file.endsWith('.js'));
if (pages.length < 12) fail('expected functional page scripts');
const pageBundle = pages.map(file => fs.readFileSync(file, 'utf8')).join('\n');
['try', 'catch', 'finally', 'Loading.show', 'Loading.hide', 'Toast.error', 'correlationId'].forEach(token => {
  if (!pageBundle.includes(token) && !ajax.includes(token)) fail(`page flow missing ${token}`);
});
const apiBundle = walk(path.join(webJs, 'api')).map(file => fs.readFileSync(file, 'utf8')).join('\n');
['/auth/login','/auth/register-company','/auth/forgot-password','/auth/reset-password','/me','/plans/public','/public/surveys/','/public/results/','/health','/admin/migration/status'].forEach(endpoint => {
  if (!apiBundle.includes(endpoint)) fail(`missing endpoint ${endpoint}`);
});
console.log('OK validate-aspnet-web-functional-flow');
