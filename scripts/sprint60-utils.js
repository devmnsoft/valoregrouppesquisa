const fs=require('fs');
function read(p){return fs.existsSync(p)?fs.readFileSync(p,'utf8'):''}
function ok(cond,msg,fail){if(!cond)fail.push(msg)}
function report(name,fail){if(fail.length){console.error(`${name}: FAIL\n- ${fail.join('\n- ')}`);process.exit(1)}console.log(`${name}: PASS`)}
module.exports={read,ok,report,exists:fs.existsSync};
