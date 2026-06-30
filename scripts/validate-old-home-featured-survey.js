const fs=require('fs');
function read(p){return fs.existsSync(p)?fs.readFileSync(p,'utf8'):''}
function ok(cond,msg){if(!cond){console.error(msg);process.exitCode=1}}
const app=read('app.js'), repo=read('firebase-repository.js'), fns=read('functions/index.js'), build=read('scripts/build-production.js');
const home=(app.match(/function renderHome\(\)[\s\S]*?function officialPublicPricingPlans/)||[''])[0];
ok(home&&/Diagnóstico gratuito Valora Insight/.test(home),'home real sem diagnóstico gratuito');
ok(!/survey_demo|empresa-exemplo|tokenHash=|aaf0854759683092b6394542f8ce5b38143dae6bf9019b6d/.test(home),'CTA da home contém link/token demo proibido');
ok(/resolveFeaturedHomeSurvey|getFeaturedHomeSurveyUrl/.test(app),'função central de featured home ausente');
ok(/registerCompany[\s\S]*organizations[\s\S]*companies/.test(repo),'provider Firebase sem registerCompany compatível');
ok(/lookupCnpj=onCall[\s\S]*req\.auth\?\.uid\|\|ip\(req\)\|\|'anon'/.test(fns),'lookupCnpj ainda exige auth ou não tem rate limit público');
ok(/lastLookupCnpj[\s\S]*nonBlocking:true/.test(app),'lookupCnpj 401/falha sem tratamento não bloqueante no front');
ok(/isDemoPublicSurveyLink\(safePayload\)/.test(app)&&/resolveOfficialFreeSurveyPayload/.test(app),'submit não redireciona survey_demo');
ok(/currentUser\(\)[\s\S]*renderTakeSurvey/.test(app)&&!/goMyArea[\s\S]{0,300}renderTakeSurvey/.test(app),'usuário logado pode ser bloqueado na pesquisa pública');
ok(app.indexOf('submitPublicSurveyViaFirestoreFallback')<app.indexOf("error.code='provider_unavailable'"),'provider_unavailable pode ocorrer antes do fallback');
ok(/localScripts/.test(build)&&/config\.js/.test(build)&&/jsBundle/.test(build)&&/app\.js/.test(read('index.html'))&&/firebase-repository\.js/.test(read('index.html')),'build não empacota arquivos legados corrigidos');
if(process.exitCode)process.exit(process.exitCode);console.log('validate-old-home-featured-survey: PASS');
