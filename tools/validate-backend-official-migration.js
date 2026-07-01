const fs = require('fs');
const path = require('path');
const root = process.cwd();
const errors = [];
const ok = (cond, msg) => { if (!cond) errors.push(msg); };
const read = f => fs.existsSync(f) ? fs.readFileSync(f, 'utf8') : '';
const walk = d => fs.existsSync(d) ? fs.readdirSync(d, {withFileTypes:true}).flatMap(e => {
  const p = path.join(d, e.name);
  if (e.isDirectory()) return walk(p);
  return [p];
}) : [];

ok(fs.existsSync('backend/Valora.sln'), 'backend/Valora.sln não existe');
ok(fs.existsSync('backend/Valora.Api/Controllers'), 'Controllers oficiais não existem');
ok(fs.existsSync('backend/Valora.Infrastructure/Repositories'), 'Repositories oficiais não existem');
ok(fs.existsSync('backend/Valora.Application/Services'), 'Services oficiais não existem');
ok(fs.existsSync('backend/Valora.Web/Valora.Web.csproj'), 'Web oficial não existe');

const pkg = JSON.parse(read('package.json') || '{}');
ok(pkg.scripts && pkg.scripts['backend:official-validate'] === 'node tools/validate-backend-official-migration.js', 'package.json sem backend:official-validate');
const officialBuildScripts = Object.entries(pkg.scripts || {}).filter(([name]) => /^(backend|web|api|prod|migration|postgres|architecture|cutover)/.test(name));
for (const [name, cmd] of officialBuildScripts) {
  if (name.startsWith('backend-v2')) continue;
  ok(!/backend-v2\//.test(cmd), `script oficial ${name} aponta para backend-v2`);
}

const apiControllers = walk('backend/Valora.Api/Controllers').filter(f => f.endsWith('.cs'));
const repos = walk('backend/Valora.Infrastructure/Repositories').filter(f => f.endsWith('.cs'));
const services = walk('backend/Valora.Application/Services').filter(f => f.endsWith('.cs'));
['AuthController','OrganizationsController','SurveysController','PublicSurveysController','PublicResultsController','PlansController','ResponsesController'].forEach(n => ok(apiControllers.some(f => f.endsWith(n+'.cs')), `controller ausente: ${n}`));
['OrganizationRepository','UserRepository','FormRepository','SurveyRepository','ResponseRepository','ResultRepository','AuditRepository','PlanRepository','CommunicationRepository','CertificateRepository'].forEach(n => ok(repos.some(f => f.endsWith(n+'.cs')), `repository ausente: ${n}`));
['AuthService','PublicSurveyService','PublicResultService','PlanEntitlementService','AuditService'].forEach(n => ok(services.some(f => f.endsWith(n+'.cs')), `service ausente: ${n}`));

const backendCode = apiControllers.concat(walk('backend/Valora.Application').filter(f=>f.endsWith('.cs'))).map(read).join('\n');
['password_hash','token_hash','result_token_hash'].forEach(s => {
  const exposed = new RegExp(`\\b(public|record|class)[^\\n;]*(?:${s})`, 'i').test(backendCode);
  ok(!exposed, `DTO/controller pode expor ${s}`);
});

const sourceForFake = apiControllers.concat(walk('backend/Valora.Web').filter(f=>/\.(cs|cshtml|js)$/.test(f)));
for (const f of sourceForFake) {
  const t = read(f);
  ok(!/fake data|mock data|dados fake|MOCK_PRODUCTION/i.test(t), `${f} contém marcador de dados fake`);
}

const gaps = read('ASPNET_WEB_API_GAPS.md');
const gapOccurrences = walk('backend').filter(f=>/\.(cs|cshtml|js)$/.test(f)).filter(f=>read(f).includes('WEB_ADMIN_REAL_REPOSITORY_REQUIRED'));
if (gapOccurrences.length) ok(gaps.includes('WEB_ADMIN_REAL_REPOSITORY_REQUIRED'), 'WEB_ADMIN_REAL_REPOSITORY_REQUIRED existe sem gap documentado');

const sql = read('database/postgresql/scriptbd_completo.sql') + '\n' + read('scriptbd_completo.sql');
['organizations','users','roles','permissions','forms','questions','question_options','surveys','survey_links','responses','response_answers','result_scores','certificates','communications','email_jobs','audit_logs','plans','plan_limits','plan_capabilities','modules','organization_modules','subscriptions','usage_monthly'].forEach(t => ok(new RegExp(`CREATE TABLE IF NOT EXISTS\\s+valorapesquisa\\.${t}\\b`, 'i').test(sql), `SQL oficial sem tabela ${t}`));

if (errors.length) {
  console.error('validate-backend-official-migration: FAIL');
  for (const e of errors) console.error('- ' + e);
  process.exit(1);
}
console.log('validate-backend-official-migration: PASS');
