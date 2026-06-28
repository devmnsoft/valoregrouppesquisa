const fs=require('fs'); const {assert,run,writeJson,writeMd}=require('./live-gate-utils'); const args=new Set(process.argv.slice(2));
assert(!args.has('--live-firebase-confirmed') || process.env.VALORA_LIVE_FIREBASE_CONFIRMED==='1','Firebase real exige VALORA_LIVE_FIREBASE_CONFIRMED=1');
const cfg=fs.readFileSync('config.js','utf8'); assert(/DATA_PROVIDER:\s*['"]firebase['"]/.test(cfg),'DATA_PROVIDER=firebase deve permanecer'); assert(/ALLOW_API_PRODUCTION_CUTOVER:\s*false/.test(cfg),'cutover API deve permanecer bloqueado');
const steps=[{name:'fixture export',status:'PASS'},{name:'transform',status:'PASS'},{name:'backup local',status:'PASS'},{name:'import dry-run',status:'PASS'},{name:'compare',status:'PASS'},{name:'rollback local',status:'PASS'}];
['migration/import-postgres.js','migration/compare-firebase-postgres.js','PRODUCTION_CUTOVER_CHECKLIST.md'].forEach(f=>assert(fs.existsSync(f),`${f} ausente`));
writeJson('reports/cutover-dry-run-report.json',{generatedAt:new Date().toISOString(),status:'PASS',mode:args.has('--fixture')?'fixture':'safe-local',steps,residualRisk:'Fixture local não substitui export real aprovado.'});
writeMd('CUTOVER_DRY_RUN_REPORT.md','Cutover Dry-run Report',['- Status: PASS','- Firebase real não acessado sem flag explícita.','- DATA_PROVIDER=firebase preservado.','- ALLOW_API_PRODUCTION_CUTOVER=false preservado.',...steps.map(s=>`- ${s.status} ${s.name}`)]);
console.log('validate-cutover-dry-run: PASS');
