const fs = require('fs');
const path = require('path');

const root = path.resolve(__dirname, '..');

const canonical = path.join(root, 'backend/database/postgresql/script_completo.sql');
if (fs.existsSync(canonical)) {
  const canonicalSql = fs.readFileSync(canonical, 'utf8');
  const checks = [
    ['schema valorapesquisa', /CREATE SCHEMA IF NOT EXISTS valorapesquisa/i],
    ['plans canonicos', /CREATE TABLE IF NOT EXISTS plans[\s\S]*code text NOT NULL UNIQUE[\s\S]*is_public boolean[\s\S]*is_active boolean[\s\S]*is_legacy boolean/i],
    ['plan_limits canonico', /CREATE TABLE IF NOT EXISTS plan_limits[\s\S]*limit_key[\s\S]*limit_value/i],
    ['plan_capabilities canonico', /CREATE TABLE IF NOT EXISTS plan_capabilities[\s\S]*capability_key[\s\S]*enabled/i],
    ['seeds oficiais', /'free'[\s\S]*'professional'[\s\S]*'corporate'[\s\S]*'enterprise'/i],
    ['idempotencia', /ON CONFLICT/i],
  ];
  let failedCanonical = false;
  for (const [name, re] of checks) {
    if (re.test(canonicalSql)) console.log(`OK ${name}`);
    else { console.error(`FAIL ${name}`); failedCanonical = true; }
  }
  if (/DROP\s+(TABLE|SCHEMA)/i.test(canonicalSql)) { console.error('FAIL sem DROP destrutivo'); failedCanonical = true; }
  else console.log('OK sem DROP destrutivo');
  if (failedCanonical) process.exit(1);
  console.log('validate-backend-official-sql-schema: PASS canonical');
  process.exit(0);
}
const postgresDir = path.join(root, 'backend/database/postgresql');
const files = [
  'script_completo.sql',
  'backend/database/postgresql/script_completo.sql',
  ...fs.readdirSync(postgresDir)
    .filter((f) => f.endsWith('.sql'))
    .map((f) => `backend/database/postgresql/${f}`),
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
let fullSql = files.map((f) => `-- ${f}\n${sql[f]}`).join('\n');
const official = `${sql['script_completo.sql']}\n${sql['backend/database/postgresql/script_completo.sql']}\n${sql['backend/database/postgresql/003_plan_tables.sql']}`;
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

const planColumns = extractCreateTableColumns(official, 'plans') || extractCreateTableColumns(official, 'valorapesquisa.plans') || new Set();
const planInserts = extractInsertColumns(fullSql, 'valorapesquisa.plans').concat(extractInsertColumns(fullSql, 'plans'));
const hasStructuredLimits = /CREATE TABLE IF NOT EXISTS\s+(?:valorapesquisa\.)?plan_limits[\s\S]*limit_key[\s\S]*limit_value/i.test(official);
const hasCapabilityCode = /CREATE TABLE IF NOT EXISTS\s+(?:valorapesquisa\.)?plan_capabilities[\s\S]*capability_key[\s\S]*enabled/i.test(official);
const orgUsesPlanCode = /CREATE TABLE IF NOT EXISTS\s+valorapesquisa\.organizations[\s\S]*plan_code\s+text/i.test(official);

ok(/CREATE TABLE IF NOT EXISTS\s+(?:valorapesquisa\.)?plans[\s\S]*id\s+uuid[\s\S]*code\s+text\s+NOT NULL\s+UNIQUE/i.test(official), 'plans usa id uuid e code textual único');
ok(planColumns.size > 0, 'schema real de plans foi localizado nos scripts oficiais');
const requiredPlanColumns = sql['backend/database/postgresql/script_completo.sql'] ? ['code', 'name', 'is_public', 'is_active', 'is_legacy', 'updated_at'] : ['code', 'name', 'description', 'monthly_price', 'annual_price', 'display_order', 'status', 'updated_at'];
for (const column of requiredPlanColumns) {
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
ok(sql['backend/database/postgresql/script_completo.sql'] ? /INSERT\s+INTO\s+plans\s*\([\s\S]*\bcode\b[\s\S]*is_public[\s\S]*is_active/i.test(official) : /INSERT\s+INTO\s+valorapesquisa\.plans\s*\([\s\S]*\bcode\b[\s\S]*monthly_price[\s\S]*annual_price/i.test(official), sql['backend/database/postgresql/script_completo.sql'] ? 'seed de plans usa code/is_public/is_active' : 'seed de plans usa code/monthly_price/annual_price');
ok(hasStructuredLimits, 'plan_limits usa limit_key/limit_value canonicos');
ok(hasCapabilityCode, 'plan_capabilities usa capability_key canonico');
if (orgUsesPlanCode) ok(!/organizations\s*\([^)]*plan_id/i.test(fullSql), 'organizations não recebe plan_id quando schema usa plan_code');
ok(/ON CONFLICT\s*\(code\)\s*DO UPDATE/i.test(official), 'plans idempotente por code');
ok(/ON CONFLICT\s*\(plan_id\)\s*DO UPDATE/i.test(official), 'plan_limits idempotente por plan_id');
ok(/ON CONFLICT\s*\(plan_id\s*,\s*capability_code\)\s*DO UPDATE/i.test(official), 'plan_capabilities idempotente por capability_code');
ok(/ON CONFLICT\s*\(slug\)\s*DO UPDATE/i.test(official), 'organização Valora idempotente por slug');
ok(/ON CONFLICT\s*\(organization_id\)\s*DO UPDATE/i.test(official), 'assinatura idempotente por organization_id');
process.exit(failed ? 1 : 0);
