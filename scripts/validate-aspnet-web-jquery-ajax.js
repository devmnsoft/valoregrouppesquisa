const fs=require('fs');
const path=require('path');
const root=process.cwd();
function has(p){return fs.existsSync(path.join(root,p));}
function read(p){return fs.readFileSync(path.join(root,p),'utf8');}
function fail(m){console.error(m);process.exit(1);}
const c=read('backend/Valora.Web/wwwroot/js/api/ajax-client.js'); if(!c.includes('$.ajax'))fail('$.ajax required'); if(/fetch\s*\(/.test(c))fail('fetch not allowed'); ['Authorization','X-Correlation-Id','timeout','401','403','422'].forEach(x=>{if(!c.includes(x))fail('missing '+x)});
console.log('OK '+__filename);
