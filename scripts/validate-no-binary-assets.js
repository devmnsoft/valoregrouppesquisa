const fs=require('fs');
const path=require('path');
const root=process.cwd();
function has(p){return fs.existsSync(path.join(root,p));}
function read(p){return fs.readFileSync(path.join(root,p),'utf8');}
function fail(m){console.error(m);process.exit(1);}
const bad=/\.(jpg|jpeg|png|ico|webp|gif|bmp|pdf)$/i; function walk(d){for(const e of fs.readdirSync(d,{withFileTypes:true})){const p=path.join(d,e.name); if(e.isDirectory())walk(p); else if(bad.test(e.name))fail('binary asset not allowed '+p)}} walk(path.join(root,'backend/Valora.Web'));
console.log('OK '+__filename);
