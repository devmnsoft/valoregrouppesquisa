#!/usr/bin/env node
const {validateFirebaseAdminCredentials}=require('./firebase-admin-client');
const r=validateFirebaseAdminCredentials();
console.log(JSON.stringify(r,null,2));
if(!r.ok&&r.status!=='SKIPPED_CREDENTIALS_MISSING')process.exit(1);
