const fs=require('fs');
function read(p){return fs.readFileSync(p,'utf8');}
function assert(cond,msg){if(!cond){console.error(`FAIL: ${msg}`);process.exitCode=1;}}
function has(p,re,msg){assert(re.test(read(p)),`${p}: ${msg}`);}
module.exports={read,assert,has};
