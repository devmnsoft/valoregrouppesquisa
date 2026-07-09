#!/usr/bin/env node
const fs=require('fs');const app=fs.readFileSync('app.js','utf8');const report=fs.existsSync('report-service.js')?fs.readFileSync('report-service.js','utf8'):'';function fail(m){console.error('FAIL:',m);process.exit(1)}
if(!/function radarBarPdfSafe/.test(app+report))fail('radarBarPdfSafe ausente');
if(/[█░→]/.test(report))fail('relatório PDF contém caracteres unicode inseguros');
if(/\?\?\?\?\?/.test(app+report))fail('código contém marcador ?????');
console.log('legacy radar pdf safe: PASS');
