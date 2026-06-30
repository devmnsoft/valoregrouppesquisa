#!/usr/bin/env node
const fs=require('fs');
const app=fs.readFileSync('app.js','utf8');
const seed=fs.readFileSync('scripts/ensure-official-free-survey.js','utf8');
const pkg=JSON.parse(fs.readFileSync('package.json','utf8'));
const fail=[];
function req(cond,msg){if(!cond)fail.push(msg);}
req(app.includes("const OFFICIAL_FREE_SURVEY_ID='official_free_survey'"),'contrato OFFICIAL_FREE_SURVEY_ID ausente');
req(app.includes('getOfficialFreeSurveyUrl'),'home não usa getOfficialFreeSurveyUrl');
const renderHome=(app.match(/function renderHome\(\)[\s\S]*?function officialPublicPricingPlans/)||[''])[0];
req(!/survey_demo/.test(renderHome),'renderHome contém survey_demo');
req(!/empresa-exemplo/.test(renderHome),'renderHome contém empresa-exemplo');
req(!/aaf0854759683092b6394542f8ce5b38143dae6bf9019b6d/.test(renderHome),'renderHome contém token demo hardcoded');
req(/getOfficialFreeSurveyUrl\(\)/.test(renderHome),'renderHome não chama getOfficialFreeSurveyUrl');
req(seed.includes('OFFICIAL_FREE_SURVEY_ID')&&seed.includes('official_free_survey'),'seed oficial ausente');
req(seed.includes('publicToken:token')&&seed.includes('tokenHash:sha256(token)'),'seed não garante publicToken/tokenHash');
req(seed.includes('tokenFromSurvey(existing)||publicToken()'),'seed não preserva publicToken válido');
req(!/tokenHash.*URL|tokenHash.*searchParams\.set\('token'/.test(seed),'seed pode usar tokenHash como URL pública');
req(app.includes('resolveOfficialFreeSurveyPayload')&&app.includes('isDemoPublicSurveyLink'),'URL demo não resolve pesquisa oficial');
req(app.includes('submitPublicSurveyViaFirestoreFallback')&&app.includes('source:\'legacy_firestore_fallback\''),'fallback Firestore oficial ausente');
req(app.includes("error.code='provider_unavailable'")&&app.indexOf('submitPublicSurveyViaFirestoreFallback')<app.indexOf("error.code='provider_unavailable'"),'provider_unavailable pode ocorrer antes do fallback');
req(pkg.scripts['seed:official-free-survey']==='node scripts/ensure-official-free-survey.js','script seed:official-free-survey ausente');
req(pkg.scripts['home:official-free-survey']==='node scripts/validate-home-official-free-survey.js','script home:official-free-survey ausente');
if(fail.length){console.error(fail.join('\n'));process.exit(1);}console.log('validate-home-official-free-survey: PASS');
