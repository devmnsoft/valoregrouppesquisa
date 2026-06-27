const fs = require('fs');
const path = require('path');
function read(file){return fs.readFileSync(path.join(process.cwd(),file),'utf8');}
function exists(file){return fs.existsSync(path.join(process.cwd(),file));}
function assert(cond,msg){if(!cond){console.error(`FAIL: ${msg}`);process.exitCode=1;}}
function pass(name){if(!process.exitCode) console.log(`${name}: PASS`);}
module.exports={read,exists,assert,pass};
