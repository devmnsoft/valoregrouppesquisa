const fs = require('fs');
const path = require('path');

const root = process.cwd();
const required = [
  'tools/windows/backend-prd-01-build-release.bat',
  'tools/windows/backend-prd-02-publish-api.bat',
  'tools/windows/backend-prd-03-publish-web.bat',
  'tools/windows/backend-prd-04-package-release.bat',
  'tools/windows/backend-prd-05-healthcheck.bat',
  'tools/linux/backend-prd-01-build-release.sh',
  'tools/linux/backend-prd-02-publish-api.sh',
  'tools/linux/backend-prd-03-publish-web.sh',
  'tools/linux/backend-prd-04-package-release.sh',
  'tools/linux/backend-prd-05-healthcheck.sh',
  'RELEASE_CANDIDATE_NOTES.md',
  'VERSION',
  'PERFORMANCE_HOMOLOGATION_REPORT.md',
  'PILOT_USERS_HOMOLOGATION_PLAN.md',
  'BACKUP_RESTORE_RUNBOOK.md',
  'CUTOVER_PLAN.md',
  'ROLLBACK_PLAN.md',
  'SPRINT_BACKEND_OFICIAL_RELEASE_CANDIDATE_DIAGNOSTIC.md',
  'SPRINT_BACKEND_OFICIAL_RELEASE_CANDIDATE_AUDIT.md',
];
const failures = [];
function read(file) { return fs.readFileSync(path.join(root, file), 'utf8'); }
function exists(file) { return fs.existsSync(path.join(root, file)); }
for (const file of required) if (!exists(file)) failures.push(`Arquivo obrigatório ausente: ${file}`);
if (exists('VERSION') && read('VERSION').trim() !== '0.9.0-rc1') failures.push('VERSION deve conter 0.9.0-rc1');
const apiHealth = exists('backend/Valora.Api/Controllers/HealthController.cs') ? read('backend/Valora.Api/Controllers/HealthController.cs') : '';
for (const route of ['/health', '/health/database', '/health/migration', '/health/email', '/health/storage', '/health/version']) {
  if (!apiHealth.includes(route)) failures.push(`Health endpoint ausente: ${route}`);
}
const webOps = exists('backend/Valora.Web/Controllers/OperationsController.cs') ? read('backend/Valora.Web/Controllers/OperationsController.cs') : '';
for (const route of ['Operations/Health', 'Operations/Version', 'Operations/Checks']) {
  if (!webOps.includes(route)) failures.push(`Rota MVC operacional ausente: ${route}`);
}
const sln = exists('backend/Valora.sln') ? read('backend/Valora.sln') : '';
if (sln.includes('backend-v2')) failures.push('backend-v2 não pode participar da solution oficial');
const forbiddenPackagePatterns = [/\.env$/i, /\.dump$/i, /\.bak$/i, /secret/i, /password/i, /connectionstring/i];
for (const dir of ['publish/package', 'publish/valora-0.9.0-rc1']) {
  const abs = path.join(root, dir);
  if (!fs.existsSync(abs)) continue;
  const stack = [abs];
  while (stack.length) {
    const current = stack.pop();
    for (const entry of fs.readdirSync(current, { withFileTypes: true })) {
      const full = path.join(current, entry.name);
      const rel = path.relative(root, full);
      if (entry.isDirectory()) stack.push(full);
      if (entry.isFile() && forbiddenPackagePatterns.some((rx) => rx.test(rel))) failures.push(`Artefato potencialmente sensível no pacote: ${rel}`);
    }
  }
}
const docs = ['RELEASE_CANDIDATE_NOTES.md', 'PERFORMANCE_HOMOLOGATION_REPORT.md', 'PILOT_USERS_HOMOLOGATION_PLAN.md'];
for (const doc of docs) if (exists(doc) && !read(doc).includes('0.9.0-rc1')) failures.push(`${doc} deve citar 0.9.0-rc1`);
if (failures.length) {
  console.error('validate-backend-official-release-candidate: FAIL');
  for (const failure of failures) console.error(`- ${failure}`);
  process.exit(1);
}
console.log('validate-backend-official-release-candidate: PASS');
