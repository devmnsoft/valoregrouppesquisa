const fs=require('fs');
function read(p){return fs.readFileSync(p,'utf8');}
function assert(c,m){if(!c){console.error('FAIL:',m);process.exitCode=1;}else console.log('OK:',m)}
function has(p,re){return re.test(read(p));}
function finish(){if(process.exitCode)process.exit(1);console.log('Validation passed.');}
module.exports={read,assert,has,finish,fs};
