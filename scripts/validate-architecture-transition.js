#!/usr/bin/env node
const fs=require('fs');
function has(path){return fs.existsSync(path)}
function read(path){return has(path)?fs.readFileSync(path,'utf8'):''}
const checks=[
 ['backend existe',has('backend/Valora.sln')&&has('backend/Valora.Api/Program.cs')],
 ['docker compose postgres existe',/postgres:16/.test(read('docker-compose.postgres.yml'))],
 ['database scripts existem',['001_create_schemas.sql','002_core_tables.sql','003_plan_tables.sql','004_survey_tables.sql','005_response_tables.sql','006_certificate_tables.sql','007_communication_tables.sql','008_audit_tables.sql','009_seed_official_plans.sql'].every(f=>has(`database/postgresql/${f}`))],
 ['API health existe',/\/health/.test(read('backend/Valora.Api/Controllers/HealthController.cs'))],
 ['schema tem tabelas principais',/valora\.organizations/.test(read('database/postgresql/002_core_tables.sql'))&&/billing\.plans/.test(read('database/postgresql/003_plan_tables.sql'))&&/valora\.responses/.test(read('database/postgresql/005_response_tables.sql'))],
 ['seed dos planos existe',['free','essential','professional','corporate','enterprise'].every(p=>read('database/postgresql/009_seed_official_plans.sql').includes(`'${p}'`))],
 ['frontend ainda tem firebase provider',/DATA_PROVIDER:\s*'firebase'/.test(read('config.js'))&&has('firebase-repository.js')],
 ['frontend tem api provider',has('api-client.js')&&has('api-repository.js')&&/api-client\.js/.test(read('index.html'))],
 ['não há segredo SMTP no frontend',!/SMTP_PASS|SMTP_PASSWORD|WHATSAPP_TOKEN/.test(read('config.js')+read('app.js'))],
 ['migração tem plano e mapping',has('MIGRATION_FIREBASE_TO_POSTGRESQL_PLAN.md')&&has('MIGRATION_FIELD_MAPPING.md')]
];
const failed=checks.filter(([,ok])=>!ok); checks.forEach(([n,ok])=>console.log(`${ok?'OK':'FAIL'} - ${n}`)); if(failed.length) process.exit(1);
