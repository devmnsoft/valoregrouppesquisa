#!/usr/bin/env node
const fs=require('fs');const fn=fs.readFileSync('functions/index.js','utf8');function fail(m){console.error('FAIL:',m);process.exit(1)}
['function buildPublicSurveyUrl','public_token_is_hash','missing_public_token','PUBLIC_APP_URL','expiresAt:safeExpiresAt','status:\'published\'','visibility:\'public\'','showResult:true','allowRepeat:true','revoked:false'].forEach(x=>{if(!fn.includes(x))fail(`${x} missing`)});
if(/url\.searchParams\.set\('token',survey\.tokenHash/.test(fn))fail('survey link uses tokenHash');
console.log('OK legacy survey link not expired');
