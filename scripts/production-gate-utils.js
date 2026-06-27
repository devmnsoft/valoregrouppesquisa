const fs = require('fs');
const path = require('path');
const root = path.resolve(__dirname, '..');
function full(p){ return path.join(root,p); }
function exists(p){ return fs.existsSync(full(p)); }
function read(p){ return fs.readFileSync(full(p),'utf8'); }
function readIf(p){ return exists(p)?read(p):''; }
function allFiles(dirs, exts){ const out=[]; const skip=new Set(['node_modules','.git','dist','publish','exports','valoregrouppesquisa.rar']); function walk(rel){ const abs=full(rel); if(!fs.existsSync(abs)) return; const st=fs.statSync(abs); if(st.isDirectory()){ if(skip.has(path.basename(rel))) return; for(const n of fs.readdirSync(abs)) walk(path.join(rel,n)); } else if(!exts || exts.includes(path.extname(rel).toLowerCase())) out.push(rel.replace(/\\/g,'/')); } dirs.forEach(walk); return out; }
function corpus(dirs, exts){ return allFiles(dirs,exts).map(f=>`\n/* ${f} */\n`+read(f)).join('\n'); }
function assert(cond,msg){ if(!cond) throw new Error(msg); }
function hasIn(files, regex){ return files.some(f=>regex.test(readIf(f))); }
function requireFile(p){ assert(exists(p), `${p} missing`); return read(p); }
function requirePattern(label, files, regex){ assert(hasIn(files, regex), `${label} missing (${regex})`); }
function scanForbiddenFrontendSecrets(files){ const bad=[]; for(const f of files){ const s=readIf(f); if(/private_key|service_account|smtp(password|_password)\s*[:=]\s*['\"][^'\"]{8,}|connectionstring\s*[:=]\s*['\"][^'\"]{12,}/i.test(s)) bad.push(f); } assert(!bad.length, `frontend secret markers found: ${bad.join(', ')}`); }
function pass(name){ console.log(`${name}: PASS`); }
module.exports={root,full,exists,read,readIf,allFiles,corpus,assert,hasIn,requireFile,requirePattern,scanForbiddenFrontendSecrets,pass};
