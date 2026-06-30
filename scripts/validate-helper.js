const fs=require('fs');
function read(f){return fs.readFileSync(f,'utf8')}
function ok(cond,msg){if(!cond){console.error(msg);process.exitCode=1}}
function done(name){if(process.exitCode)process.exit(process.exitCode);console.log(`${name}: PASS`)}
module.exports={read,ok,done};
