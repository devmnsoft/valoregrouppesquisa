const fs=require('fs');const s=fs.readFileSync('functions/index.js','utf8');
function fail(m){throw new Error(m)}
const list=s.match(/const ALLOWED_CORS_ORIGINS\s*=\s*\[([\s\S]*?)\];/);if(!list)fail('ALLOWED_CORS_ORIGINS ausente');
for(const x of ['https://valoragroup.mnsoft.com.br','https://valorateste.mnsoft.com.br','https://gestordepesquisa.web.app','https://gestordepesquisa.firebaseapp.com','localhost','127\\.0\\.0\\.1'])if(!list[0].includes(x))fail('origin ausente: '+x);
if(!/exports\.adminDeleteResponse\s*=\s*onCall\s*\(\s*\{[^}]*cors\s*:\s*ALLOWED_CORS_ORIGINS/s.test(s))fail('adminDeleteResponse sem cors ALLOWED_CORS_ORIGINS');
console.log('validate-functions-admin-delete-response-cors: PASS');
