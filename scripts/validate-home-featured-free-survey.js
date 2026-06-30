#!/usr/bin/env node
const fs=require('fs');
const app=fs.readFileSync('app.js','utf8');
const pkg=JSON.parse(fs.readFileSync('package.json','utf8'));
function fail(msg){console.error(`home-featured-free-survey: FAIL - ${msg}`);process.exit(1)}
function req(cond,msg){if(!cond)fail(msg)}
const renderHome=(app.match(/function renderHome\(\)[\s\S]*?function officialPublicPricingPlans/)||[''])[0];
req(!/survey_demo/.test(renderHome),'home contém CTA ou bloco com survey_demo');
req(!/empresa-exemplo/.test(renderHome),'home contém empresa-exemplo');
req(!/aaf0854759683092b6394542f8ce5b38143dae6bf9019b6d|tokenHash=/.test(renderHome),'home contém token demo/tokenHash hardcoded');
req(/function resolveHomeFeaturedSurvey|function resolveFeaturedHomeSurvey|function getOfficialFreeSurveyUrl/.test(app),'função resolvedora de pesquisa destaque ausente');
req(/featuredOnHome[\s\S]*isFeatured/.test(app)&&/name="featuredOnHome"/.test(app),'fluxo admin sem featuredOnHome/isFeatured');
req(pkg.scripts&&pkg.scripts['seed:official-free-survey'],'script seed:official-free-survey ausente');
req(/isDemoPublicSurveyLink\(submitPayload\)[\s\S]*legacy_demo_payload_blocked/.test(app),'submit pode enviar survey_demo');
req(/const user=currentUser\(\)\?\.role==='participante'\?currentUser\(\):null;/.test(app)===false,'usuário logado não participante ainda é bloqueado no preenchimento');
req(app.indexOf('submitPublicSurveyViaFirestoreFallback')<app.indexOf("error.code='provider_unavailable'"),'provider_unavailable ocorre antes de fallback Firestore');
req(/result\?\.resultToken\|\|result\?\.accessToken/.test(app),'normalizePublicSubmitResult não aceita accessToken');
console.log('home-featured-free-survey: PASS');
