#!/usr/bin/env node
const fs=require('fs');
const required=['MIGRATION_FIREBASE_TO_POSTGRESQL_PLAN.md','MIGRATION_FIELD_MAPPING.md','MIGRATION_RISK_REGISTER.md'];
const missing=required.filter(f=>!fs.existsSync(f));
if(missing.length){ console.error('Arquivos ausentes:', missing.join(', ')); process.exit(1); }
console.log('Plano de migração Firebase → PostgreSQL validado.');
