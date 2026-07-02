const fs = require('fs');
function read(file){return fs.readFileSync(file,'utf8');}
function assert(condition,message){if(!condition){console.error('FAIL:',message);process.exit(1);}}
function ok(message){console.log('OK:',message);}
module.exports={read,assert,ok};
