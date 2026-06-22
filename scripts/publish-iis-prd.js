#!/usr/bin/env node
const fs = require('fs');
const path = require('path');
const cp = require('child_process');

const root = path.resolve(__dirname, '..');
const stamp = new Date().toISOString().replace(/[-:T]/g, '').slice(0, 12);
const report = { checks: [], errors: [], js: [], css: [], backup: '', packagePath: '', dataImport: 'não solicitada', healthcheck: 'não executado', files: [], securitySkipped: false };
function parseArgs(argv){const out={};for(let i=0;i<argv.length;i++){const a=argv[i];if(!a.startsWith('--'))continue;const k=a.slice(2).replace(/-([a-z])/g,(_,c)=>c.toUpperCase());if(['dryRun','apply','packageOnly','withData','skipBuild','skipSecurityCheck','noHealthCheck'].includes(k))out[k]=true;else out[k]=argv[++i];}if(!out.apply&&!out.packageOnly)out.dryRun=true;return out;}
function readJson(file, fallback={}){try{return JSON.parse(fs.readFileSync(path.join(root,file),'utf8'));}catch(_){return fallback;}}
const args=parseArgs(process.argv.slice(2));
const cfg={projectName:'Valora Pulse',mode:'firebase',firebaseProjectId:'gestordepesquisa',productionUrl:'https://valoragroup.mnsoft.com.br',iisPath:'C:\\inetpub\\wwwroot\\valoragroup',backupPath:'backups/iis',reportsPath:'publish/reports',packagePath:'publish/packages',runHealthCheck:true,preserveFiles:['web.config'],withData:false,dataFile:'',...readJson('publish.config.json')};
const mode=args.mode||cfg.mode; const project=args.project||cfg.firebaseProjectId; const url=args.url||args.healthUrl||cfg.productionUrl; const iisPath=args.iisPath||cfg.iisPath; const dist=path.join(root,'dist');
function cfgPath(v,fallback){return path.resolve(root,String(v||fallback).replace(/\\/g,path.sep));}
const reportsDir=cfgPath(cfg.reportsPath,'publish/reports'); const backupRoot=cfgPath(cfg.backupPath,'backups/iis'); const packagesRoot=cfgPath(cfg.packagePath,'publish/packages');
function log(m){console.log(m);} function fail(m){report.errors.push(m);throw new Error(m);} function check(name,ok,details=''){report.checks.push({name,status:ok?'OK':'FALHA',details}); if(!ok)fail(`${name}: ${details}`); log(`${ok?'[OK]':'[FALHA]'} ${name}${details?` - ${details}`:''}`);} 
function run(cmd, optional=false){log(`$ ${cmd}`);try{cp.execSync(cmd,{cwd:root,stdio:'inherit',shell:true});return true;}catch(e){if(optional){report.checks.push({name:cmd,status:'AVISO',details:e.message});return false;}throw e;}}
function runSecurityCheck(){
  if(args.skipSecurityCheck){
    report.securitySkipped=true;
    report.checks.push({name:'npm run security:check',status:'AVISO',details:'ATENÇÃO: security-check foi pulado nesta publicação.'});
    console.warn('ATENÇÃO: security-check foi pulado nesta publicação.');
    return;
  }
  log('$ npm run security:check');
  try{
    cp.execSync('npm run security:check',{cwd:root,stdio:'inherit',shell:true});
  }catch(e){
    const msg=String(e.message||'');
    if(/spawnSync git ENOENT|Git não foi encontrado|Git não encontrado/i.test(msg)){
      console.error('O security-check falhou porque o Git não foi encontrado.');
      console.error('Instale o Git:');
      console.error('winget install --id Git.Git -e --source winget');
      console.error('');
      console.error('Ou adicione ao PATH:');
      console.error('C:\\Program Files\\Git\\cmd');
      console.error('');
      console.error('Para continuar emergencialmente, execute novamente com --skip-security-check.');
    }
    throw e;
  }
}
function ensure(rel){const p=path.join(root,rel);check(rel,fs.existsSync(p),'arquivo obrigatório');return p;}
function configText(){return fs.readFileSync(path.join(root,'config.js'),'utf8');}
function validateFirebase(){const txt=configText(); if(mode!=='firebase')return; const storage=/STORAGE_MODE\s*:\s*['"]firebase['"]/.test(txt); const enabled=/FIREBASE_ENABLED\s*:\s*true/.test(txt); const pid=/projectId\s*:\s*['"]([^'"]+)['"]/.exec(txt)?.[1]; const required=['apiKey','authDomain','projectId','appId'].every(k=>new RegExp(`${k}\\s*:\\s*['\"][^'\"]+`).test(txt)); if(!storage||!enabled||!pid||pid!==project||!required)fail(`Firebase PRD não configurado. Preencha config.js antes de publicar. Esperado STORAGE_MODE=firebase, FIREBASE_ENABLED=true e projectId=${project}.`);}
function walk(dir){if(!fs.existsSync(dir))return [];return fs.readdirSync(dir,{withFileTypes:true}).flatMap(e=>{const p=path.join(dir,e.name);return e.isDirectory()?walk(p):[p];});}
function refs(html){return [...html.matchAll(/(?:src|href)=["'](?:\.\/)?(assets\/[^"']+\.(?:js|css))["']/g)].map(m=>m[1]);}
function looksHtml(file){const b=fs.readFileSync(file,'utf8').slice(0,200).toLowerCase();return b.includes('<!doctype')||b.includes('<html');}
function validateBuild(dir=dist,label='dist'){check(`${label}/index.html`,fs.existsSync(path.join(dir,'index.html')),'index deve existir');check(`${label}/assets`,fs.existsSync(path.join(dir,'assets')),'assets deve existir');const files=walk(dir).map(f=>path.relative(dir,f).replace(/\\/g,'/'));const js=files.filter(f=>/^assets\/.+\.js$/.test(f));const css=files.filter(f=>/^assets\/.+\.css$/.test(f));check(`${label} JS`,js.length>0,'ao menos um JS');check(`${label} CSS`,css.length>0,'ao menos um CSS');for(const f of [...js,...css])check(`${label} MIME ${f}`,!looksHtml(path.join(dir,f)),'JS/CSS não pode ser HTML');const bad=files.filter(f=>/\.map$|(^|\/)\.env(\.|$)|serviceAccount|telegram.*token|smtp.*password|secret|backups\/|exports\//i.test(f));check(`${label} sem artefatos proibidos`,bad.length===0,bad.join(', ')||'ok');const html=fs.readFileSync(path.join(dir,'index.html'),'utf8');for(const r of refs(html))check(`${label} referência ${r}`,fs.existsSync(path.join(dir,r)),'arquivo referenciado existe');check(`${label}/web.config`,fs.existsSync(path.join(dir,'web.config')),'web.config deve existir');report.js=js; report.css=css; report.files=files;return {js,css,files};}
function copyDir(src,dst){fs.mkdirSync(dst,{recursive:true});for(const e of fs.readdirSync(src,{withFileTypes:true})){const s=path.join(src,e.name),d=path.join(dst,e.name);if(e.isDirectory())copyDir(s,d);else fs.copyFileSync(s,d);}}
function empty(dir){fs.mkdirSync(dir,{recursive:true});for(const e of fs.readdirSync(dir))fs.rmSync(path.join(dir,e),{recursive:true,force:true});}
function backup(){const dest=path.join(backupRoot,`valoragroup-${stamp}`);fs.mkdirSync(dest,{recursive:true});if(fs.existsSync(iisPath)&&fs.readdirSync(iisPath).length)copyDir(iisPath,dest);report.backup=path.relative(root,dest);return dest;}
function writeWebConfig(){const tpl=path.join(root,'templates/iis/web.config');check('templates/iis/web.config',fs.existsSync(tpl),'template IIS');fs.copyFileSync(tpl,path.join(dist,'web.config'));}
function packageOnly(){const dest=path.join(packagesRoot,`valoragroup-iis-prd-${stamp}`);fs.rmSync(dest,{recursive:true,force:true});copyDir(dist,dest);report.packagePath=path.relative(root,dest);log(`Pacote IIS gerado: ${report.packagePath}`);}
function importData(){if(!(args.withData||cfg.withData))return; const file=args.dataFile||cfg.dataFile; if(!file)fail('--with-data exige --data-file.'); if(!fs.existsSync(path.resolve(root,file)))fail(`Arquivo de dados não encontrado: ${file}`); run(`node scripts/import-local-export-to-firebase.js --file "${file}" --project "${project}" --dry-run`); if(args.apply){run(`node scripts/import-local-export-to-firebase.js --file "${file}" --project "${project}" --apply --backup`); run(`node scripts/validate-prd-data.js --project "${project}"`); report.dataImport='importado e validado';}else report.dataImport='dry-run executado';}
function health(){if(args.noHealthCheck||cfg.runHealthCheck===false)return; const cmd=`node scripts/healthcheck-prd.js --url "${url}" --project "${project}" --check-firebase`; try{run(cmd);report.healthcheck='OK';}catch(e){report.healthcheck='FALHOU'; console.error('Publicação copiada, mas health check falhou. Verifique relatório e considere rollback.');}}
function writeReport(status){fs.mkdirSync(reportsDir,{recursive:true});const version=/APP_VERSION\s*:\s*['"]([^'"]+)/.exec(configText())?.[1]||'n/a';const body=[`# Relatório de publicação PRD/IIS`, '', `- Data: ${new Date().toISOString()}`, `- Modo: ${mode}`, `- URL: ${url}`, `- IIS Path: ${iisPath}`, `- Firebase Project: ${project}`, `- Build Version: ${version}`, `- Arquivos gerados: ${(report.js||[]).concat(report.css||[]).join(', ')||'n/a'}`, `- Backup criado: ${report.backup||'não'}`, `- Pacote: ${report.packagePath||'não'}`, `- Importação de dados: ${report.dataImport}`, `- Health check: ${report.healthcheck}`, `- Security check pulado: ${report.securitySkipped?'sim — ATENÇÃO: security-check foi pulado nesta publicação.':'não'}`, `- Status final: ${status}`,  '', '## Checks', ...report.checks.map(c=>`- ${c.status}: ${c.name}${c.details?` — ${c.details}`:''}`), '', '## Próximos passos', '- Se o health check falhou, confira logs do IIS e considere `node scripts/restore-iis-backup.js --latest`.', '- Não commite dist/, backups/, exports/ ou publish/reports/.', ''].join('\n');const file=path.join(reportsDir,`publicacao-prd-${stamp}.md`);fs.writeFileSync(file,body);log(`Relatório final: ${path.relative(root,file)}`);}
try{process.chdir(root);ensure('package.json');ensure('index.html');ensure('config.js');ensure('scripts/build-production.js');ensure('publish.config.json'); if(args.apply)fs.mkdirSync(iisPath,{recursive:true}); validateFirebase(); importData(); run('npm run check'); runSecurityCheck(); if(!args.skipBuild)run('npm run build:prod'); writeWebConfig(); validateBuild(dist,'dist'); if(args.packageOnly)packageOnly(); else if(args.apply){backup(); empty(iisPath); copyDir(dist,iisPath); validateBuild(iisPath,'IIS'); health(); log('Publicação IIS concluída.');} else {log(`Dry-run concluído. Plano validado para IIS: ${iisPath}. Nada foi copiado.`);} writeReport(report.healthcheck==='FALHOU'?'SUCESSO_COM_ALERTA':'SUCESSO');}
catch(e){console.error(`\nERRO: ${e.message}`);try{writeReport('FALHA');}catch(_){}process.exit(1);}
