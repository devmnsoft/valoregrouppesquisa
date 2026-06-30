#!/usr/bin/env node
const fs=require('fs');
function read(f){return fs.readFileSync(f,'utf8')}
function assert(c,m){if(!c){console.error('FAIL:',m);process.exitCode=1}}
['SPRINT_55_LEGACY_MOBILE_MENU_FINAL_AUDIT.md','LEGACY_ADMIN_MENU_GAPS.md','ADMIN_MOBILE_MENU_RUNTIME_FIX.md','LEGACY_INDEX_TO_ASPNET_WEB_CORRECTION_PARITY.md','FREE_SURVEY_EXPIRATION_POLICY.md','PUBLIC_SURVEY_LINK_REPAIR.md','LEGACY_EMAIL_REAL_RESPONSE_FLOW.md','CLIENT_CREATE_FLOW.md','FIREBASE_AUTH_CREATE_USER_FLOW.md','KNOWN_LIMITATIONS_BEFORE_PRODUCTION.md'].forEach(f=>assert(fs.existsSync(f),f+' ausente'));
const app=read('app.js'); assert(app.includes('getAdminMenuItems'),'menu não centralizado'); assert(app.includes('createCompanyUser')||read('firebase-repository.js').includes('createCompanyUser'),'createCompanyUser ausente');
if(process.exitCode)process.exit(1);
console.log('OK validate-legacy-index-corrections-parity.js');
