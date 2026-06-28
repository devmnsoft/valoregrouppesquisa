const fs=require('fs');
const path=require('path');
const root=process.cwd();
function has(p){return fs.existsSync(path.join(root,p));}
function read(p){return fs.readFileSync(path.join(root,p),'utf8');}
function fail(m){console.error(m);process.exit(1);}
const c=read('backend/Valora.Api/Configuration/ApiServiceCollectionExtensions.cs')+read('backend/Valora.Api/Program.cs'); ['http://localhost:5088','http://127.0.0.1:5088','UseCors'].forEach(x=>{if(!c.includes(x))fail('missing CORS '+x)});
console.log('OK '+__filename);
