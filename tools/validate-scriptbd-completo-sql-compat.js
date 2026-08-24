#!/usr/bin/env node
const fs = require('fs');
const canonical = 'backend/database/postgresql/script_completo.sql';
let failed = false;
const fail = message => { console.error(`❌ ${message}`); failed = true; };
const ok = message => console.log(`✅ ${message}`);
if (!fs.existsSync(canonical)) { fail(`script oficial ausente: ${canonical}`); process.exit(1); }
const sql = fs.readFileSync(canonical, 'utf8');
const has = expression => expression.test(sql);
const before = (compatibility, use) => sql.search(compatibility) >= 0 && sql.search(compatibility) < sql.search(use);
if (sql.indexOf('-- COMPATIBILIDADE PARA BANCOS EXISTENTES') < 0) fail('fase explícita de compatibilidade ausente.'); else ok('fase explícita de compatibilidade encontrada.');
const contracts = {
  api_keys: ['organization_id','name','key_prefix','key_hash','scopes','status','last_used_at','use_count','created_at','updated_at','revoked_at','deleted_at'],
  forms: ['name','title','description','organization_id','status','created_at','updated_at','deleted_at'],
  questions: ['organization_id','form_id','form_version_id','dimension_id','code','title','text','description','type','min_value','max_value','weight','position','display_order','is_required','required','is_active','is_qualitative','version','created_at','updated_at','deleted_at']
};
for (const [table, columns] of Object.entries(contracts)) for (const column of columns) {
  const alteration = new RegExp(`ALTER\\s+TABLE\\s+valorapesquisa\\.${table}\\s+ADD\\s+COLUMN\\s+IF\\s+NOT\\s+EXISTS\\s+${column}\\b`, 'i');
  if (!has(alteration)) fail(`${table}.${column} não é convergida com ADD COLUMN IF NOT EXISTS.`);
}
if (!before(/ALTER TABLE valorapesquisa\.api_keys ADD COLUMN IF NOT EXISTS key_hash/i, /CREATE UNIQUE INDEX IF NOT EXISTS ux_api_keys_hash/i)) fail('api_keys.key_hash não é convergida antes do índice único.'); else ok('api_keys converge antes do índice único.');
if (!/FOREACH legacy_column IN ARRAY ARRAY\['secret_hash','hash','api_key_hash'\]/.test(sql)) fail('migração segura dos hashes legados ausente.'); else ok('hashes legados são migrados dinamicamente.');
if (!/row_number\(\) OVER \(PARTITION BY key_hash/i.test(sql)) fail('deduplicação canônica de key_hash ausente.'); else ok('key_hash é deduplicada antes do índice.');
if (!/compatible_scale_question_type\(\)/.test(sql) || !/likert_1_5/.test(sql)) fail('tipo compatível de pergunta não é resolvido pelo CHECK.'); else ok('tipo de pergunta respeita CHECK legado.');
if (!/admin_valora/.test(sql)) fail('papel global admin_valora ausente.');
else if (/superadmin@valoragroup\.local/.test(sql) || /\$2[aby]\$\d\d\$/.test(sql)) fail('credencial administrativa estática não pode ser seedada no schema canônico.');
else ok('papel admin_valora existe sem credencial administrativa estática.');
if (/Valora Grup(?!o)/.test(sql)) fail('marca incorreta encontrada.'); else ok('marca Valora Group consistente.');
if (!failed) ok(`Validador do SQL canônico concluiu sem erros: ${canonical}`);
process.exit(failed ? 1 : 0);
