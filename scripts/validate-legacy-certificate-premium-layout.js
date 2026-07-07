#!/usr/bin/env node
const fs=require('fs');const app=fs.readFileSync('app.js','utf8');const pdf=fs.readFileSync('pdf.js','utf8');function fail(m){console.error('FAIL:',m);process.exit(1)}
if(/createCertificate[\s\S]{0,1200}Valora Pulse™/.test(pdf))fail('certificate PDF still uses Valora Pulse™');
if(!/const W=842,H=595/.test(pdf))fail('certificate PDF is not A4 landscape');
const body=app.slice(app.indexOf('async function certificatePdf'), app.indexOf('function reportRows'));
if(!body.includes('loadPublicResultBundleForAction'))fail('certificatePdf does not use fallback bundle loader');
console.log('OK legacy certificate premium layout');
