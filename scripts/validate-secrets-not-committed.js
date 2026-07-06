const fs=require('fs'),cp=require('child_process');
const files=cp.execSync('git ls-files',{encoding:'utf8'}).trim().split(/\n/).filter(Boolean);
const bad=[];
for(const f of files){
  if(/package-lock\.json$/.test(f))continue;
  const t=fs.readFileSync(f,'utf8');
  const lines=t.split(/\n/);
  lines.forEach((line,i)=>{
    const isPatternSource=/validate|security-check|migration-logger/.test(f)&&/RegExp|pattern|SECRET_PATTERNS|SMTP_PASSWORD\\s|SMTP_PASS\\s|BEGIN PRIVATE KEY/.test(line);
    if(isPatternSource)return;
    if(/SMTP_PASSWORD\s*=\s*['"][^'"]{8,}|SMTP_PASS\s*=\s*['"][^'"]{8,}|-----BEGIN PRIVATE KEY-----/.test(line))bad.push(`${f}:${i+1}`);
  });
}
if(bad.length)throw new Error('possíveis segredos commitados: '+bad.join(', '));
console.log('secrets not committed: PASS');
