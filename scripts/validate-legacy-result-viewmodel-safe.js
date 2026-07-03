const fs=require('fs');const s=fs.readFileSync('app.js','utf8');function fail(m){console.error(m);process.exit(1)}
if(!s.includes('function normalizePublicResultViewModel'))fail('normalizePublicResultViewModel não existe');
const r=s.slice(s.indexOf('async function renderResult'),s.indexOf('function isDemoCompany', s.indexOf('async function renderResult')));
if(!r.includes('normalizePublicResultViewModel'))fail('renderResult não usa view model');
if(/payload\.company\.(publicName|name)/.test(r))fail('renderResult acessa payload.company profundo sem optional chaining');
console.log('OK result viewmodel safe');
