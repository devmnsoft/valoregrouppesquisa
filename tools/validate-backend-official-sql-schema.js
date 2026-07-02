const fs = require('fs');
const path = require('path');
const root = path.resolve(__dirname, '..');
const files = [
  'scriptbd_completo.sql',
  'database/postgresql/scriptbd_completo.sql',
  ...fs.readdirSync(path.join(root, 'database/postgresql')).filter(f => f.endsWith('.sql')).map(f => `database/postgresql/${f}`),
];
let failed = false;
function ok(cond, msg) { if (cond) console.log(`OK ${msg}`); else { console.error(`FAIL ${msg}`); failed = true; } }
const sql = Object.fromEntries(files.map(f => [f, fs.readFileSync(path.join(root, f), 'utf8')]));
const official = `${sql['scriptbd_completo.sql']}\n${sql['database/postgresql/scriptbd_completo.sql']}\n${sql['database/postgresql/003_plan_tables.sql']}`;
const hasStructuredLimits = /CREATE TABLE IF NOT EXISTS\s+valorapesquisa\.plan_limits[\s\S]*active_surveys[\s\S]*responses_per_month[\s\S]*storage_mb/i.test(official);
const hasCapabilityCode = /CREATE TABLE IF NOT EXISTS\s+valorapesquisa\.plan_capabilities[\s\S]*capability_code[\s\S]*enabled/i.test(official);
const orgUsesPlanCode = /CREATE TABLE IF NOT EXISTS\s+valorapesquisa\.organizations[\s\S]*plan_code\s+text/i.test(official);
ok(/CREATE TABLE IF NOT EXISTS\s+valorapesquisa\.plans[\s\S]*id\s+uuid[\s\S]*code\s+text\s+NOT NULL\s+UNIQUE/i.test(official), 'plans usa id uuid e code textual único');
ok(!/price_label/i.test(official), 'SQL oficial não referencia price_label');
ok(!/INSERT\s+INTO\s+valorapesquisa\.plans\s*\(\s*id\s*,/i.test(official), 'seed não usa plans(id, ...) para código textual');
ok(/INSERT\s+INTO\s+valorapesquisa\.plans\s*\([\s\S]*\bcode\b[\s\S]*monthly_price[\s\S]*annual_price/i.test(official), 'seed de plans usa code/monthly_price/annual_price');
if (hasStructuredLimits) ok(!/plan_limits\s*\([^)]*limit_key[^)]*limit_value/i.test(official), 'plan_limits não usa limit_key/limit_value');
if (hasCapabilityCode) ok(!/plan_capabilities\s*\([^)]*capability_key/i.test(official), 'plan_capabilities não usa capability_key/capability_level/capability_type');
if (orgUsesPlanCode) ok(!/organizations\s*\([^)]*plan_id/i.test(official), 'organizations não recebe plan_id quando schema usa plan_code');
ok(/ON CONFLICT\s*\(code\)\s*DO UPDATE/i.test(official), 'plans idempotente por code');
ok(/ON CONFLICT\s*\(plan_id\)\s*DO UPDATE/i.test(official), 'plan_limits idempotente por plan_id');
ok(/ON CONFLICT\s*\(plan_id\s*,\s*capability_code\)\s*DO UPDATE/i.test(official), 'plan_capabilities idempotente por capability_code');
ok(/ON CONFLICT\s*\(slug\)\s*DO UPDATE/i.test(official), 'organização Valora idempotente por slug');
ok(/ON CONFLICT\s*\(organization_id\)\s*DO UPDATE/i.test(official), 'assinatura idempotente por organization_id');
process.exit(failed ? 1 : 0);
