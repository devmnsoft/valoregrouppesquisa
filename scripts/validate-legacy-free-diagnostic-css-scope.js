const fs=require('fs');const path=require('path');
const css=fs.readFileSync('style.css','utf8');const app=fs.readFileSync('app.js','utf8');
function readDist(dir){let out='';if(!fs.existsSync(dir))return out;for(const name of fs.readdirSync(dir)){const file=path.join(dir,name);const st=fs.statSync(file);if(st.isDirectory())out+=readDist(file);else if(/\.(html|js|css)$/.test(file))out+=fs.readFileSync(file,'utf8')+'\n';}return out;}const dist=readDist('dist');
function fail(m){console.error(`validate-legacy-free-diagnostic-css-scope: FAIL - ${m}`);process.exit(1)}
if(!/@media \(max-width:760px\)/.test(css))fail('media max-width 760px ausente');
if(!/@media \(max-width:420px\)/.test(css))fail('media max-width 420px ausente');
if(!/\.free-diagnostic-layout\{[^}]*grid-template-columns:minmax\(0,1\.05fr\) minmax\(360px,\.95fr\)/.test(css))fail('grid desktop de duas colunas ausente');
if(/free-diagnostic-mobile-card/.test(app))fail('classe mobile proibida no app');
const built=dist||app+'\n'+css;
for(const c of ['free-diagnostic-section','free-diagnostic-layout','free-diagnostic-card','free-diagnostic-preview-card']) if(!built.includes(c))fail(`build/fontes sem classe corrigida ${c}`);
console.log('validate-legacy-free-diagnostic-css-scope: PASS');
