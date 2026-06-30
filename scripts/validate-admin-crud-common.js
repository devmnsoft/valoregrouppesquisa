const fs=require('fs');
function read(f){return fs.readFileSync(f,'utf8');}
function fail(m){console.error('FAIL:',m);process.exitCode=1;}
function between(src,start,end){const i=src.indexOf(start);if(i<0)return '';const j=end?src.indexOf(end,i+start.length):-1;return src.slice(i,j<0?src.length:j);}
module.exports={read,fail,between};
