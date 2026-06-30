const fs=require('fs');const path=require('path');
function read(f){return fs.readFileSync(path.join(process.cwd(),f),'utf8')}
function assert(c,m){if(!c){console.error('FAIL:',m);process.exitCode=1}}
function has(f,s){return read(f).includes(s)}
function regex(f,r){return r.test(read(f))}
function done(name){if(process.exitCode)process.exit(process.exitCode);console.log(`${name}: OK`)}
module.exports={read,assert,has,regex,done};
