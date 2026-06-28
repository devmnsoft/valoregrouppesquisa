const fs=require('fs');
const path=require('path');
const root=process.cwd();
function has(p){return fs.existsSync(path.join(root,p));}
function read(p){return fs.readFileSync(path.join(root,p),'utf8');}
function fail(m){console.error(m);process.exit(1);}
if(!read('backend/Valora.Web/Views/Shared/_Layout.cshtml').includes('viewport'))fail('viewport missing'); if(!read('backend/Valora.Web/Views/Shared/_Sidebar.cshtml').includes('offcanvas'))fail('mobile menu missing');
console.log('OK '+__filename);
