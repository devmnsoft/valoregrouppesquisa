#!/usr/bin/env node
const fs = require('fs');
const path = require('path');

const files = [
  'scriptbd_completo.sql',
  'backend/database/postgresql/scriptbd_completo.sql',
  ...fs.readdirSync('backend/database/postgresql').filter(f => f.endsWith('.sql')).map(f => path.join('backend/database/postgresql', f)),
];
let failed = false;
const fail = (m) => { console.error(`❌ ${m}`); failed = true; };
const ok = (m) => console.log(`✅ ${m}`);
const read = (f) => fs.readFileSync(f, 'utf8');
const main = read('scriptbd_completo.sql');

function before(a,b){ const ia=main.indexOf(a), ib=main.indexOf(b); return ia>=0 && ib>=0 && ia<ib; }
if (!main.includes('-- COMPATIBILIDADE PARA BANCOS EXISTENTES')) fail('scriptbd_completo.sql não contém bloco oficial de compatibilidade.'); else ok('bloco oficial de compatibilidade encontrado.');
[
 ['plan_limits.users', 'ALTER TABLE valorapesquisa.plan_limits ADD COLUMN IF NOT EXISTS users', 'INSERT INTO valorapesquisa.plan_limits'],
 ['forms.name', 'ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS name', 'INSERT INTO valorapesquisa.forms'],
 ['questions.display_order', 'ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS display_order', 'INSERT INTO valorapesquisa.questions'],
 ['question_options.score', 'ALTER TABLE valorapesquisa.question_options ADD COLUMN IF NOT EXISTS score', 'INSERT INTO valorapesquisa.question_options'],
 ['email_templates.body_html', 'ALTER TABLE valorapesquisa.email_templates ADD COLUMN IF NOT EXISTS body_html', 'INSERT INTO email_templates(code,name,subject,body_html'],
].forEach(([label, alter, insert]) => before(alter, insert) ? ok(`${label} é adicionada antes do seed.`) : fail(`${label} não é adicionada antes do seed.`));

if (/usage_monthly\s*\(\s*organization_id\s*,\s*month\s*\)/i.test(files.map(read).join('\n'))) fail('índice usage_monthly ainda usa month.'); else ok('usage_monthly usa period_month.');
if (/\bprice_label\b/i.test(files.map(read).join('\n'))) fail('price_label encontrado nos scripts SQL.'); else ok('price_label ausente.');
if (/\bbadge\b/i.test(files.map(read).join('\n'))) fail('badge encontrado nos scripts SQL.'); else ok('badge ausente.');
if (/service_account|private_key|client_email|BEGIN PRIVATE KEY|firebase-adminsdk/i.test(files.map(read).join('\n'))) fail('possível service account/secret encontrado.'); else ok('nenhum service account/secret óbvio encontrado.');
if (/ON CONFLICT\s*\(\s*code\s*,\s*organization_id\s*\)/i.test(files.map(read).join('\n'))) fail('email_templates ainda usa ON CONFLICT (code, organization_id).'); else ok('email_templates não usa ON CONFLICT (code, organization_id).');
if (/ - pergunta \s*'|pergunta '\s*\|\|/i.test(main)) fail('seed Valora Insight contém perguntas genéricas.'); else ok('seed Valora Insight não contém perguntas genéricas.');

const compatSection = main.slice(main.indexOf('-- COMPATIBILIDADE PARA BANCOS EXISTENTES'), main.indexOf('DROP TRIGGER IF EXISTS'));
const requiredCols = {
  plan_limits: ['plan_id','active_surveys','responses_per_month','users','managers','forms','public_links','email_invites_per_month','storage_mb','created_at','updated_at'],
  plan_capabilities: ['plan_id','capability_code','enabled','created_at','updated_at'],
  plans: ['code','name','description','monthly_price','annual_price','display_order','status','created_at','updated_at'],
  email_templates: ['code','organization_id','name','from_email','subject','body','body_html','body_text','status','is_deleted','created_at','updated_at'],
};
for (const [table, cols] of Object.entries(requiredCols)) {
  for (const col of cols) {
    const re = new RegExp(`ALTER\\s+TABLE\\s+valorapesquisa\\.${table}\\s+ADD\\s+COLUMN\\s+IF\\s+NOT\\s+EXISTS\\s+${col}\\b`, 'i');
    if (col === 'plan_id' && table === 'plan_limits' && /CREATE TABLE IF NOT EXISTS valorapesquisa\.plan_limits[^;]*plan_id/i.test(main)) continue;
    if (!re.test(compatSection) && !(table==='plans' && col==='code' && /CREATE TABLE IF NOT EXISTS valorapesquisa\.plans[^;]*code/i.test(main))) fail(`compatibilidade não garante ${table}.${col}.`);
  }
}
if (!failed) ok('Validador SQL compat concluiu sem erros.');
process.exit(failed ? 1 : 0);
