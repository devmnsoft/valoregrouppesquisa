const fs=require('fs');const app=fs.readFileSync('app.js','utf8'),css=fs.readFileSync('style.css','utf8');
const req=['free-diagnostic-section','free-diagnostic-layout','free-diagnostic-card','free-diagnostic-copy','free-diagnostic-benefits','free-diagnostic-start-card','Responder diagnóstico gratuito'];
const miss=req.filter(x=>!app.includes(x));if(miss.length)throw new Error('app.js sem '+miss.join(','));
if(app.includes('free-diagnostic-mobile-card'))throw new Error('app.js não deve usar free-diagnostic-mobile-card');
if(!/@media \(max-width:760px\)\{[\s\S]*\.free-diagnostic-copy h2,\.free-diagnostic-title\{[^}]*font-size:clamp\(1\.55rem,8vw,2\.2rem\)/.test(css)||!/@media \(max-width:420px\)/.test(css))throw new Error('CSS mobile do diagnóstico gratuito incompleto ou fora do escopo');
console.log('validate-legacy-free-diagnostic-mobile-copy: PASS');
