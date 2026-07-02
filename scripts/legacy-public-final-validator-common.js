const fs=require('fs');
function read(p){return fs.readFileSync(p,'utf8');}
function must(label,ok){if(!ok){console.error(`FAIL: ${label}`);process.exit(1);}console.log(`OK: ${label}`);}
module.exports={read,must};
