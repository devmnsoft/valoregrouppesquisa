const fs = require('fs');
const path = require('path');

const root = path.resolve(__dirname, '..');
const postgresDir = path.join(root, 'database/postgresql');
const files = [
  'scriptbd_completo.sql',
  'database/postgresql/scriptbd_completo.sql',
  ...fs.readdirSync(postgresDir)
    .filter((f) => f.endsWith('.sql'))
    .map((f) => `database/postgresql/${f}`),
];

let failed = false;
function ok(cond, msg) {
  if (cond) console.log(`OK ${msg}`);
  else {
    console.error(`FAIL ${msg}`);
    failed = true;
  }
}

const sql = Object.fromEntries(files.map((f) => [f, fs.readFileSync(path.join(root, f), 'utf8')]));
const fullSql = files.map((f) => `-- ${f}\n${sql[f]}`).join('\n');
const official = `${sql['scriptbd_completo.sql']}\n${sql['database/postgresql/scriptbd_completo.sql']}\n${sql['database/postgresql/003_plan_tables.sql']}`;
const forbiddenPlanColumns = [
  'price_label',
  'badge',
  'public_subtitle',
  'public_description',
  'highlight_text',
  'cta_label',
];

function extractCreateTableColumns(sqlText, tableName) {
  const escaped = tableName.replace('.', '\\.');
  const match = sqlText.match(new RegExp(`CREATE TABLE IF NOT EXISTS\\s+${escaped}\\s*\\(([\\s\\S]*?)\\);`, 'i'));
  if (!match) return null;
  return new Set(match[1]
    .split(/,(?![^()]*\))/)
    .map((part) => part.trim().match(/^"?([a-zA-Z_][\w]*)"?\b/))
    .filter(Boolean)
    .map((m) => m[1].toLowerCase())
    .filter((column) => !['primary', 'unique', 'constraint', 'foreign', 'check'].includes(column)));
}

function extractInsertColumns(sqlText, tableName) {
  const escaped = tableName.replace('.', '\\.');
  return [...sqlText.matchAll(new RegExp(`INSERT\\s+INTO\\s+${escaped}\\s*\\(([^)]*)\\)`, 'gi'))]
    .map((match) => match[1].split(',').map((column) => column.trim().replace(/^"|"$/g, '').toLowerCase()));
}

const planColumns = extractCreateTableColumns(official, 'valorapesquisa.plans') || new Set();
const planInserts = extractInsertColumns(fullSql, 'valorapesquisa.plans');
const hasStructuredLimits = /CREATE TABLE IF NOT EXISTS\s+valorapesquisa\.plan_limits[\s\S]*active_surveys[\s\S]*responses_per_month[\s\S]*storage_mb/i.test(official);
const hasCapabilityCode = /CREATE TABLE IF NOT EXISTS\s+valorapesquisa\.plan_capabilities[\s\S]*capability_code[\s\S]*enabled/i.test(official);
const orgUsesPlanCode = /CREATE TABLE IF NOT EXISTS\s+valorapesquisa\.organizations[\s\S]*plan_code\s+text/i.test(official);

ok(/CREATE TABLE IF NOT EXISTS\s+valorapesquisa\.plans[\s\S]*id\s+uuid[\s\S]*code\s+text\s+NOT NULL\s+UNIQUE/i.test(official), 'plans usa id uuid e code textual único');
ok(planColumns.size > 0, 'schema real de plans foi localizado nos scripts oficiais');
for (const column of ['code', 'name', 'description', 'monthly_price', 'annual_price', 'display_order', 'status', 'updated_at']) {
  ok(planColumns.has(column), `plans contém coluna real ${column}`);
}
for (const column of forbiddenPlanColumns) {
  ok(!planColumns.has(column), `schema de plans não cria coluna inexistente ${column}`);
  ok(!new RegExp(`\\b${column}\\b`, 'i').test(fullSql), `SQL oficial não referencia ${column}`);
}
ok(planInserts.length > 0, 'existe seed oficial para valorapesquisa.plans');
planInserts.forEach((columns, index) => {
  const unknown = columns.filter((column) => !planColumns.has(column));
  ok(unknown.length === 0, `INSERT #${index + 1} em plans usa apenas colunas reais${unknown.length ? `: ${unknown.join(', ')}` : ''}`);
  ok(columns.includes('code'), `INSERT #${index + 1} em plans usa code`);
  ok(!columns.includes('id'), `INSERT #${index + 1} em plans não faz seed textual em id uuid`);
});
ok(/INSERT\s+INTO\s+valorapesquisa\.plans\s*\([\s\S]*\bcode\b[\s\S]*monthly_price[\s\S]*annual_price/i.test(official), 'seed de plans usa code/monthly_price/annual_price');
if (hasStructuredLimits) ok(!/plan_limits\s*\([^)]*limit_key[^)]*limit_value/i.test(fullSql), 'plan_limits não usa limit_key/limit_value');
if (hasCapabilityCode) ok(!/plan_capabilities\s*\([^)]*capability_key/i.test(fullSql), 'plan_capabilities não usa capability_key/capability_level/capability_type');
if (orgUsesPlanCode) ok(!/organizations\s*\([^)]*plan_id/i.test(fullSql), 'organizations não recebe plan_id quando schema usa plan_code');
ok(/ON CONFLICT\s*\(code\)\s*DO UPDATE/i.test(official), 'plans idempotente por code');
ok(/ON CONFLICT\s*\(plan_id\)\s*DO UPDATE/i.test(official), 'plan_limits idempotente por plan_id');
ok(/ON CONFLICT\s*\(plan_id\s*,\s*capability_code\)\s*DO UPDATE/i.test(official), 'plan_capabilities idempotente por capability_code');
ok(/ON CONFLICT\s*\(slug\)\s*DO UPDATE/i.test(official), 'organização Valora idempotente por slug');
ok(/ON CONFLICT\s*\(organization_id\)\s*DO UPDATE/i.test(official), 'assinatura idempotente por organization_id');
process.exit(failed ? 1 : 0);
