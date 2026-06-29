const fs=require('fs');
function has(f,...xs){const s=fs.readFileSync(f,'utf8'); for(const x of xs) if(!s.includes(x)) throw new Error(`${f} sem ${x}`)}
function any(pattern){const {execSync}=require('child_process'); return execSync(`find . -path './node_modules' -prune -o -type f -print | xargs grep -Il "${pattern}"`,{encoding:'utf8'});}
module.exports={has,any};
