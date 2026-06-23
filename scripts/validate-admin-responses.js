#!/usr/bin/env node
const fs = require('fs');
const path = require('path');
const root = path.resolve(__dirname, '..');
const appPath = path.join(root, 'app.js');
const source = fs.readFileSync(appPath, 'utf8');
const failures = [];
function ok(name, condition, details = '') {
  if (!condition) failures.push(`${name}${details ? `: ${details}` : ''}`);
  else console.log(`[PASS] ${name}`);
}
function declared(name) {
  return new RegExp(`function\\s+${name}\\s*\\(`).test(source) || new RegExp(`(?:const|let|var)\\s+${name}\\s*=`).test(source);
}
[
  'canDeleteResponse',
  'canViewResponse',
  'canEditResponse',
  'canExportResponse',
  'canDownloadResponseCertificate',
  'resolveResponsePermissions',
  'deleteResponse'
].forEach(name => ok(`${name} declarado`, declared(name)));
ok('anonymizeResponse declarado ou botão inexistente', declared('anonymizeResponse') || !/data-action="anonymizeResponse"/.test(source));
const mini = (source.match(/function miniResponsesTable[\s\S]*?const ACTION_PRIORITY_LABELS/) || [''])[0];
ok('render de respostas usa permissões resolvidas com fallback', /resolveResponsePermissions/.test(mini) && /canView:true/.test(mini) && /canDelete\s*:\s*false/.test(mini));
ok('render de respostas não chama canDeleteResponse diretamente', !/canDeleteResponse\s*\(/.test(mini));
ok('portalTab captura falha de renderização', /function renderPortalTab[\s\S]*?try\{[\s\S]*?catch\(error\)/.test(source) && /renderPortalTabError/.test(source));
ok('portalTab exibe fallback amigável', /Não foi possível carregar esta área/.test(source) && !/canDeleteResponse is not defined/.test(source));
ok('deleteResponse aplica exclusão lógica', /status:'deleted'/.test(source) && /deleted:true/.test(source) && /deletedAt/.test(source));
if (failures.length) {
  console.error('\n[FAIL] Validação Admin Respostas encontrou problemas:');
  failures.forEach(f => console.error(`- ${f}`));
  process.exit(1);
}
console.log('\nAdmin Respostas validado com sucesso.');
