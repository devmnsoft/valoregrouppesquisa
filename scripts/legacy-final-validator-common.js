const fs=require('fs');
function read(f){return fs.readFileSync(f,'utf8');}
function must(name, ok){if(!ok)throw new Error(name);}
module.exports={read,must};
