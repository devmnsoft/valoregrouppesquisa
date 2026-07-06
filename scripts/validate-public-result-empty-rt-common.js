const fs = require('fs');
function read(file){return fs.readFileSync(file,'utf8');}
function assertIncludes(src, needle, msg){if(!src.includes(needle)){console.error(`FAIL: ${msg}\nMissing: ${needle}`);process.exit(1);}}
function assertNotIncludes(src, needle, msg){if(src.includes(needle)){console.error(`FAIL: ${msg}\nForbidden: ${needle}`);process.exit(1);}}
module.exports={read,assertIncludes,assertNotIncludes};
