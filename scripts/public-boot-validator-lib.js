const fs=require('fs');
function read(f){return fs.readFileSync(f,'utf8');}
function assert(cond,msg){if(!cond){console.error('FAIL:',msg);process.exitCode=1;}}
function done(name){if(!process.exitCode)console.log(`${name}: OK`);else process.exit(process.exitCode);}
module.exports={read,assert,done};
