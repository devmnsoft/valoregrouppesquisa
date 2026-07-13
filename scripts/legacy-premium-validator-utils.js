const fs=require('fs');
const app=fs.readFileSync('app.js','utf8');
function ok(cond,msg){if(!cond){console.error('FAIL:',msg);process.exit(1)}console.log('OK:',msg)}
function has(re,msg){ok(re.test(app),msg)}
function no(re,msg){ok(!re.test(app),msg)}
module.exports={app,ok,has,no};
