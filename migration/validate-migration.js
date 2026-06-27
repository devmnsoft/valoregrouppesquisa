#!/usr/bin/env node
'use strict';
const migrationLogger=require('./migration-logger');
async function main(){
const done=migrationLogger.time();
migrationLogger.step('validate-migration.js started');
const fs=require('fs');
const required=['MIGRATION_FIREBASE_TO_POSTGRESQL_PLAN.md','MIGRATION_FIELD_MAPPING.md','MIGRATION_RISK_REGISTER.md'];
const missing=required.filter(f=>!fs.existsSync(f));
if(missing.length){ console.error('Arquivos ausentes:', missing.join(', ')); process.exit(1); }
console.log('Plano de migração Firebase → PostgreSQL validado.');

migrationLogger.success('validate-migration.js completed',{durationMs:done()});
}
main().catch(error=>{migrationLogger.fail('validate-migration.js failed',{durationMs:0,error:migrationLogger.sanitize(error&&error.message||error)});process.exit(1);});
