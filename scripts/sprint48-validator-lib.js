const fs=require('fs');
function read(p){return fs.existsSync(p)?fs.readFileSync(p,'utf8'):''}
function must(cond,msg){if(!cond){console.error(msg);process.exitCode=1}}
function has(p,terms){const s=read(p); must(!!s,`missing ${p}`); for(const t of terms) must(s.includes(t),`${p} missing ${t}`)}
module.exports={read,must,has};
