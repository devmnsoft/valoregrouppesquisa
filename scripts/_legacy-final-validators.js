const fs=require('fs');
const read=f=>fs.existsSync(f)?fs.readFileSync(f,'utf8'):'';
function ok(cond,msg){if(!cond){console.error('FAIL:',msg);process.exitCode=1;}else console.log('OK:',msg)}
const app=read('app.js'), css=read('style.css'), fn=read('functions/index.js'), pdf=read('pdf.js'), pkg=read('package.json');
module.exports={ok,app,css,fn,pdf,pkg};
