const fs=require('fs');
function fail(m){console.error(m);process.exit(1)}
const firebase=JSON.parse(fs.readFileSync('firebase.json','utf8'));
const pkg=JSON.parse(fs.readFileSync('functions/package.json','utf8'));
const idx=fs.readFileSync('functions/index.js','utf8');
if(firebase.functions?.runtime!=='nodejs22')fail('firebase.json must use functions.runtime nodejs22');
if(String(pkg.engines?.node)!=='22')fail('functions/package.json engines.node must be 22');
for(const d of ['firebase-functions','firebase-admin','nodemailer']) if(!pkg.dependencies?.[d]) fail(`missing ${d}`);
if(!idx.includes("require('firebase-functions/v2/https')")&&!idx.includes('require("firebase-functions/v2/https")'))fail('functions/index.js must use v2 https import');
if(!/defineSecret\(['"]SMTP_PASSWORD['"]\)/.test(idx))fail('SMTP_PASSWORD must use defineSecret');
if(/SMTP_PASSWORD\s*[:=]\s*['"][^'"${\s][^'"]{8,}/.test(idx))fail('hardcoded SMTP_PASSWORD found');
console.log('functions:node22-readiness OK');
