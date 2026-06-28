const fs=require('fs');
const path=require('path');
const root=process.cwd();
function has(p){return fs.existsSync(path.join(root,p));}
function read(p){return fs.readFileSync(path.join(root,p),'utf8');}
function fail(m){console.error(m);process.exit(1);}
['auth-api.js','plans-api.js','surveys-api.js','public-survey-api.js','results-api.js','certificates-api.js','communications-api.js','audit-api.js','migration-api.js','health-api.js'].forEach(f=>{if(!has('backend/Valora.Web/wwwroot/js/api/'+f))fail('missing '+f)});
console.log('OK '+__filename);
