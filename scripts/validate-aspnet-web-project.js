const fs=require('fs');
const path=require('path');
const root=process.cwd();
function has(p){return fs.existsSync(path.join(root,p));}
function read(p){return fs.readFileSync(path.join(root,p),'utf8');}
function fail(m){console.error(m);process.exit(1);}
['backend/Valora.Web/Valora.Web.csproj','backend/Valora.Web/Program.cs','backend/Valora.Web/Views/Shared/_Layout.cshtml'].forEach(p=>{if(!has(p))fail('missing '+p)}); if(/Dapper|EntityFramework|Npgsql/.test(read('backend/Valora.Web/Valora.Web.csproj')))fail('database package not allowed');
console.log('OK '+__filename);
