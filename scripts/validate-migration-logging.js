#!/usr/bin/env node
const fs=require('fs');
const files=['migration/export-firestore.js','migration/transform-firestore-to-postgres.js','migration/import-postgres.js','migration/compare-firebase-postgres.js','migration/validate-migration.js','scripts/validate-real-migration-dry-run.js'];
const missing=[];if(!fs.existsSync('migration/migration-logger.js'))missing.push('migration/migration-logger.js');for(const f of files){const s=fs.readFileSync(f,'utf8');for(const token of ['migrationLogger','async function main','catch(error','process.exit(1)','sanitize'])if(!s.includes(token))missing.push(`${f}: ${token}`);}if(missing.length){console.error(missing.join('\n'));process.exit(1);}console.log('validate-migration-logging: PASS');
