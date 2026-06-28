const fs=require('fs');
const path=require('path');
const root=process.cwd();
function has(p){return fs.existsSync(path.join(root,p));}
function read(p){return fs.readFileSync(path.join(root,p),'utf8');}
function fail(m){console.error(m);process.exit(1);}
const files=fs.readdirSync(path.join(root,'backend/Valora.Web/wwwroot/js/pages')); for(const f of files){const c=read('backend/Valora.Web/wwwroot/js/pages/'+f); if(f!=='home-page.js'&&(!c.includes('try')||!c.includes('catch')||!c.includes('finally')))fail('handler without try/catch/finally '+f)}
console.log('OK '+__filename);
