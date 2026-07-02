const fs=require('fs');
function read(p){return fs.readFileSync(p,'utf8');}
function must(name,cond){if(!cond){console.error(`FAIL: ${name}`);process.exit(1);}console.log(`OK: ${name}`);}
module.exports={read,must};
