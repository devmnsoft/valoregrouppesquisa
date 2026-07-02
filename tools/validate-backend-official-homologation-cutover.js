#!/usr/bin/env node
const fs = require('fs');
const path = require('path');
const root = process.cwd();
const requiredFiles = [
  'HOMOLOGACAO_CUTOVER_CHECKLIST.md','CUTOVER_PLAN.md','ROLLBACK_PLAN.md','LEGACY_RETIREMENT_PLAN.md','BACKUP_RESTORE_RUNBOOK.md','SECURITY_HARDENING_CHECKLIST.md',
  'tools/windows/backend-hml-06-backup.bat','tools/windows/backend-hml-07-restore.bat','tools/linux/backend-hml-06-backup.sh','tools/linux/backend-hml-07-restore.sh',
  'backend/Valora.Api/Controllers/HealthController.cs','backend/Valora.Web/Controllers/OperationsController.cs','backend/Valora.Tests/Integration/PostgresHomologationFlowTests.cs',
  'docs/migration-samples/firestore-export-sample.json','docs/migration-samples/localstorage-export-sample.json','docs/migration-samples/manual-import-sample.json'
];
let failed = false;
function fail(msg){ console.error(`FAIL: ${msg}`); failed = true; }
function ok(msg){ console.log(`OK: ${msg}`); }
for (const file of requiredFiles) fs.existsSync(path.join(root,file)) ? ok(file) : fail(`missing ${file}`);
const pkg = JSON.parse(fs.readFileSync(path.join(root,'package.json'),'utf8'));
for (const script of ['backend:official-validate','backend:reports-email-validate','backend:migration-import-validate','backend:homologation-cutover-validate']) {
  if (!pkg.scripts || !pkg.scripts[script]) fail(`package script missing ${script}`); else ok(`package script ${script}`);
}
const health = fs.readFileSync(path.join(root,'backend/Valora.Api/Controllers/HealthController.cs'),'utf8');
for (const route of ['/health','/health/database','/health/migration','/health/email','/health/storage','/health/version']) {
  if (!health.includes(route)) fail(`health route missing ${route}`); else ok(`health route ${route}`);
}
const ops = fs.readFileSync(path.join(root,'backend/Valora.Web/Controllers/OperationsController.cs'),'utf8');
for (const route of ['Operations/Health','Operations/Version','Operations/Checks']) {
  if (!ops.includes(route)) fail(`MVC operations route missing ${route}`); else ok(`MVC route ${route}`);
}
const docs = ['README.md','backend/README.md','BACKEND_OFICIAL_MIGRATION_GUIDE.md','SAAS_FINAL_ACCEPTANCE_CHECKLIST.md','ASPNET_WEB_API_GAPS.md','ASPNET_WEB_ROUTES.md'];
for (const doc of docs) {
  const txt = fs.existsSync(path.join(root,doc)) ? fs.readFileSync(path.join(root,doc),'utf8') : '';
  if (/evoluir\s+`?backend-v2`?/i.test(txt)) fail(`${doc} appears to instruct evolving backend-v2`);
}
const officialBuildRefs = JSON.stringify(pkg.scripts).match(/backend-v2:(build|official|prod)/i);
if (officialBuildRefs) fail('backend-v2 referenced as official build path'); else ok('backend-v2 is not official build path');
if (failed) process.exit(1);
console.log('Homologation/cutover validator passed.');
