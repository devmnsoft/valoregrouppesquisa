const fs=require('fs'),path=require('path'); function walk(d){return fs.existsSync(d)?fs.readdirSync(d,{withFileTypes:true}).flatMap(e=>{const p=path.join(d,e.name);return e.isDirectory()?walk(p):[p]}):[]}
const files=walk('backend/Valora.Web').filter(f=>/\.(cshtml|js)$/.test(f)); const patterns=[/password_hash/i,/result_token_hash/i,/token_hash/i,/connection string/i,/SMTP password/i,/private_key/i,/console\.log\s*\([^)]*(token|resultToken|authorization)/i,/<pre/i,/stack trace/i]; const hits=[];
for(const f of files){const s=fs.readFileSync(f,'utf8'); patterns.forEach(r=>{if(r.test(s))hits.push(`${f}: ${r}`)});}
if(hits.length){console.error('Possível dado sensível na UI:\n'+hits.join('\n'));process.exit(1)} console.log('validate-aspnet-web-no-sensitive-ui: PASS');
