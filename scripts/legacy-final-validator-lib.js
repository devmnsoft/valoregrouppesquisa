const fs=require('fs');
function read(f){return fs.readFileSync(f,'utf8')}
function ok(cond,msg){if(!cond){throw new Error(msg)}}
function no(f,re,msg){ok(!re.test(read(f)),msg||`${f} matched ${re}`)}
function has(f,needle,msg){ok(read(f).includes(needle),msg||`${f} missing ${needle}`)}
module.exports={read,ok,no,has};
