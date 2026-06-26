#!/usr/bin/env node
'use strict';
const fs = require('fs');
const path = require('path');

const dir = 'database/postgresql';
const requiredFiles = [
  '001_create_schema_valorapesquisa.sql',
  '002_core_tables.sql',
  '003_plan_tables.sql',
  '004_survey_tables.sql',
  '005_response_tables.sql',
  '006_result_tables.sql',
  '007_certificate_tables.sql',
  '008_communication_tables.sql',
  '009_audit_tables.sql',
  '010_migration_control_tables.sql',
  '011_seed_official_plans.sql',
  '012_seed_demo_valora_insight.sql'
];
const requiredTables = [
  'organizations','users','units','plans','plan_limits','plan_capabilities','subscriptions','usage_monthly',
  'forms','form_dimensions','questions','question_options','surveys','survey_links','responses','response_answers',
  'result_scores','dimension_scores','certificates','email_jobs','whatsapp_jobs','communications','audit_logs',
  'schema_migrations','import_batches','import_errors','compare_reports'
].map(name => `valorapesquisa.${name}`);

const missingFiles = requiredFiles.filter(file => !fs.existsSync(path.join(dir, file)));
if (missingFiles.length) {
  console.error(`Arquivos ausentes: ${missingFiles.join(', ')}`);
  process.exit(1);
}

const sql = requiredFiles.map(file => fs.readFileSync(path.join(dir, file), 'utf8')).join('\n');
const missingTables = requiredTables.filter(table => !sql.includes(table));
if (missingTables.length) {
  console.error(`Tabelas ausentes: ${missingTables.join(', ')}`);
  process.exit(1);
}

console.log('validate-postgres-schema: PASS');
