#!/usr/bin/env node
const fs=require('fs');const app=fs.readFileSync('app.js','utf8');function fail(m){console.error('FAIL:',m);process.exit(1)}
['readLastPublicResultFromSession','valora:lastPublicResult','loadPublicResultBundleForAction','lastPublicResultActionFallback'].forEach(x=>{if(!app.includes(x))fail(`${x} missing`)});
const body=app.slice(app.indexOf('async function reportResponsePdf'), app.indexOf('async function downloadResultReport'));
if(!body.includes('loadPublicResultBundleForAction'))fail('reportResponsePdf does not use fallback bundle loader');
console.log('OK legacy report fallback cache');
