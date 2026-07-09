const fs=require('fs');
function read(p){return fs.readFileSync(p,'utf8')}
function assert(cond,msg){if(!cond){console.error('FAIL:',msg);process.exit(1)}}
function has(file,re,msg){assert(re.test(read(file)),`${file}: ${msg}`)}
function no(file,re,msg){assert(!re.test(read(file)),`${file}: ${msg}`)}
module.exports={read,assert,has,no};
