const fs=require('fs');
const app=fs.readFileSync('app.js','utf8');
const repo=fs.readFileSync('firebase-repository.js','utf8');
const local=fs.readFileSync('local-repository.js','utf8');
const fns=fs.readFileSync('functions/index.js','utf8');
const fail=[];
const ok=(cond,msg)=>{if(!cond)fail.push(msg)};
const home=(app.match(/function renderHome\(\)[\s\S]*?function officialPublicPricingPlans/)||[''])[0];
const seed=(app.match(/function seedStore\(\)[\s\S]*?function seedPlans/)||[''])[0];
const submit=(app.match(/async function submitPublicSurveyAuto\(payload\)[\s\S]*?async function submitPublicSurveyResponse/)||[''])[0];
ok(!/survey_demo|empresa-exemplo|tokenHash=|aaf0854759683092b6394542f8ce5b38143dae6bf9019b6d/.test(home),'CTA da home contém link/token demo proibido');
ok(/const hasFeaturedSurvey=Boolean\(featuredSurveyUrl\)/.test(home),'renderHome ainda depende de featuredSurvey && featuredSurveyUrl para habilitar CTA');
ok(/getFeaturedHomeSurveyUrl\(\)/.test(home),'renderHome não resolve URL remota da pesquisa destaque');
ok(/function registerCompany\(data/.test(repo)&&/registerCompany,/.test(repo),'provider Firebase sem registerCompany compatível');
ok(/registerCompany\(data/.test(local),'provider local sem alias registerCompany');
ok(/callFunction\('getFeaturedHomeSurvey'/.test(repo),'firebase provider não chama getFeaturedHomeSurvey');
ok(/exports\.getFeaturedHomeSurvey=onCall/.test(fns),'Cloud Function getFeaturedHomeSurvey ausente');
ok(/lookupCnpj=onCall[\s\S]*req\.auth\?\.uid\|\|ip\(req\)\|\|'anon'/.test(fns)&&!/lookupCnpj=onCall[\s\S]{0,120}authedUser\(req\)/.test(fns),'lookupCnpj ainda exige auth ou não tem rate limit público');
ok(!/companyId='org_demo',surveyId='survey_demo'/.test(seed),'seedStore usa survey_demo como pesquisa principal');
ok(/companyId='valora-oficial',surveyId='official_free_survey'/.test(seed),'seedStore não usa pesquisa oficial como principal');
ok(/isDemoPublicSurveyLink\(safePayload\)[\s\S]*resolveFeaturedHomeSurveyPayload/.test(submit),'submit pode enviar survey_demo sem redirecionar para pesquisa real');
if(fs.existsSync('dist/app.js')){const dist=fs.readFileSync('dist/app.js','utf8');const distHome=(dist.match(/function renderHome\(\)[\s\S]*?function officialPublicPricingPlans/)||[''])[0];ok(!/survey_demo|empresa-exemplo|tokenHash=|aaf0854759683092b6394542f8ce5b38143dae6bf9019b6d/.test(distHome),'dist contém survey_demo/empresa-exemplo/token demo no CTA da home');}
if(fail.length){console.error(fail.join('\n'));process.exit(1)}
console.log('validate-old-home-featured-survey: PASS');
