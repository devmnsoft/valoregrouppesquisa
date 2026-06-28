const fs=require('fs');
const path=require('path');
const root=process.cwd();
function has(p){return fs.existsSync(path.join(root,p));}
function read(p){return fs.readFileSync(path.join(root,p),'utf8');}
function fail(m){console.error(m);process.exit(1);}
if(!has('backend/Valora.Web/Dockerfile'))fail('missing Dockerfile'); if(!read('docker-compose.yml').includes('valora-web'))fail('compose missing valora-web');
console.log('OK '+__filename);
