const fs=require('fs');const path=require('path');
const roots=['backend','scripts','migration'];
const bad=['ILogger<','catch (Exception ex)','logger.LogError','LogError(ex','throw;','validate:','Sprint 24 operational logging contract'];
const exts=new Set(['.cs','.js','.ts','.json']);
function files(dir){if(!fs.existsSync(dir))return[];return fs.readdirSync(dir,{withFileTypes:true}).flatMap(d=>{const p=path.join(dir,d.name);if(d.isDirectory()&&!['node_modules','bin','obj','.git'].includes(d.name))return files(p);return d.isFile()&&exts.has(path.extname(p))?[p]:[]})}
function comments(s){return [...s.matchAll(/\/\/.*|\/\*[\s\S]*?\*\//g)].map(m=>m[0]);}
const hits=[];for(const f of roots.flatMap(files)){for(const c of comments(fs.readFileSync(f,'utf8'))){for(const b of bad)if(c.includes(b))hits.push(`${f}: comentário falso contém ${b}`)}}
if(hits.length){console.error(hits.join('\n'));process.exit(1)}console.log('validate-no-validator-fake-comments: PASS');
