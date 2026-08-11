#!/usr/bin/env node
const fs = require('fs');
const canonical = 'backend/database/postgresql/script_completo.sql';
let failed = false;
const fail = message => { console.error(`❌ ${message}`); failed = true; };
const ok = message => console.log(`✅ ${message}`);
if (!fs.existsSync(canonical)) fail(`Script canônico ausente: ${canonical}`);
const sql = fs.existsSync(canonical) ? fs.readFileSync(canonical, 'utf8') : '';
const requirePattern = (pattern, message) => pattern.test(sql) ? ok(message) : fail(message);
requirePattern(/-- COMPATIBILIDADE PARA BANCOS EXISTENTES/, 'fase inicial de compatibilidade encontrada');
const compatibility = sql.indexOf('-- COMPATIBILIDADE PARA BANCOS EXISTENTES');
const firstSeed = sql.search(/INSERT\s+INTO/i);
const firstTrigger = sql.search(/CREATE\s+TRIGGER/i);
if (compatibility >= 0 && [firstSeed, firstTrigger].filter(x => x >= 0).every(x => compatibility < x)) ok('compatibilidade antecede seeds e triggers');
else fail('compatibilidade deve anteceder seeds e triggers');
for (const column of ['organization_id','name','key_prefix','key_hash','scopes','status','last_used_at','use_count','created_at','updated_at','revoked_at','deleted_at']) {
  requirePattern(new RegExp(`ALTER TABLE valorapesquisa\\.api_keys ADD COLUMN IF NOT EXISTS ${column}\\b`, 'i'), `api_keys.${column} convergida`);
}
requirePattern(/information_schema\.columns[\s\S]*ARRAY\['secret_hash','hash','api_key_hash'\][\s\S]*EXECUTE format/i, 'hashes legados copiados somente por SQL dinâmico');
const normalizeHash = sql.indexOf('WITH duplicate_hashes');
const uniqueHash = sql.indexOf('CREATE UNIQUE INDEX IF NOT EXISTS ux_api_keys_hash');
(normalizeHash >= 0 && uniqueHash > normalizeHash) ? ok('duplicidades de key_hash normalizadas antes do índice único') : fail('normalização de key_hash deve anteceder índice único');
for (const contract of ['forms ALTER COLUMN title SET NOT NULL','questions ALTER COLUMN position SET NOT NULL','questions ALTER COLUMN display_order SET NOT NULL']) {
  sql.includes(contract) ? ok(`${contract} protegido`) : fail(`${contract} ausente`);
}
if (/INSERT\s+INTO\s+valorapesquisa\.\w+\s+VALUES/i.test(sql)) fail('seed posicional sem lista de colunas encontrado'); else ok('seeds possuem listas explícitas de colunas');
requirePattern(/VALUES\('Valora Group','valora-platform','active'\)/, 'organização oficial Valora Group configurada');
requirePattern(/superadmin@valoragroup\.local[\s\S]*\$2[ayb]\$12\$/, 'super administrador usa BCrypt cost 12');
requirePattern(/CROSS JOIN valorapesquisa\.permissions[\s\S]*WHERE r\.code='admin_valora'/, 'admin_valora recebe catálogo administrativo completo');
if (!failed) ok('Validador SQL compat concluiu sem erros.');
process.exit(failed ? 1 : 0);
