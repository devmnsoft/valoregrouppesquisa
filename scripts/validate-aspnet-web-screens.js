const fs=require('fs');
const path=require('path');
const root=process.cwd();
function has(p){return fs.existsSync(path.join(root,p));}
function read(p){return fs.readFileSync(path.join(root,p),'utf8');}
function fail(m){console.error(m);process.exit(1);}
['Account/Login','Account/Register','Dashboard/Index','Plans/Index','PublicSurvey/Take','Results/Public','Certificates/Validate'].forEach(v=>{if(!has('backend/Valora.Web/Views/'+v+'.cshtml'))fail('missing '+v)});
console.log('OK '+__filename);
