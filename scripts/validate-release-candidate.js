const { spawnSync } = require('child_process');
const fs = require('fs');
const path = require('path');
const gates = ['check','frontend:bootstrap','frontend:api-errors','security:check','validate:data-contracts','validate:render-resilience','audit:data-shapes','api:no-fake-validator-comments','api:error-handling','api:logging','api:repository-logging','api:migration-logging','api:no-sensitive-logs','api:correlation','api:transaction-logging','api:email-errors','api:health-observability','api:provider','journey:provider','api:routes','api:public-real','api:typed-services','api:service-size','api:result-token','api:transactional-submit','api:repository-transaction','architecture:warnings','cutover:ready','backend:implementation','backend:clean','postgres:schema','runtime:docker-windows','prod:saas-readiness','prod:no-legacy','prod:no-pending','prod:saas-features','prod:auth-flow','prod:certificate-flow','prod:email-flow','prod:billing','prod:security-gate','prod:frontend-saas','backend:build','backend:test','backend:health','postgres:mvp','api:e2e','prod:saas-e2e','hybrid:check','migration:dry-run-real','migration:validate','migration:compare','prod:cutover-dry-run','prod:rollback-ready','build:prod','prod:health'];
function gitCommit(){ const r=spawnSync('git',['rev-parse','--short','HEAD'],{encoding:'utf8'}); return r.status===0?r.stdout.trim():null; }
const report={generatedAt:new Date().toISOString(),environment:process.env.NODE_ENV||'local',commit:gitCommit(),gates:[],passed:[],failed:[],residualRisk:'Sem promessa de zero bug; release bloqueado por qualquer gate falho. Homologação funcional e cutover controlado continuam obrigatórios.',status:'RUNNING'};
fs.mkdirSync('reports',{recursive:true});
for (const gate of gates) {
  const started=Date.now(); console.log(`release gate: npm run ${gate}`);
  const r=spawnSync('npm',['run',gate],{stdio:'inherit',shell:process.platform==='win32'});
  const row={gate,status:r.status===0?'PASS':'FAIL',durationMs:Date.now()-started}; report.gates.push(row);
  (row.status==='PASS'?report.passed:report.failed).push(gate);
  if(r.status!==0){ report.status='FAIL'; write(); process.exit(r.status || 1); }
}
report.status='PASS'; write(); console.log('validate-release-candidate: PASS');
function write(){
 fs.writeFileSync(path.join('reports','release-candidate-report.json'), JSON.stringify(report,null,2));
 const md=['# Release Candidate Report','',`- Data/hora: ${report.generatedAt}`,`- Ambiente: ${report.environment}`,`- Commit: ${report.commit||'indisponível'}`,`- Status final: ${report.status}`,`- Risco residual: ${report.residualRisk}`,'','## Gates executados',...report.gates.map(g=>`- ${g.status==='PASS'?'PASS':'FAIL'} ${g.gate} (${g.durationMs}ms)`),'',`## Gates aprovados: ${report.passed.length}`,report.passed.map(g=>`- ${g}`).join('\n'),' ',`## Gates falhos: ${report.failed.length}`,report.failed.map(g=>`- ${g}`).join('\n')||'- Nenhum',''].join('\n');
 fs.writeFileSync('RELEASE_CANDIDATE_REPORT.md',md);
}
