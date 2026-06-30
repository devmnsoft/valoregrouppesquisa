const fs=require('fs');
const f=fs.readFileSync('functions/index.js','utf8');
const app=fs.readFileSync('app.js','utf8');
function ok(c,m){if(!c)throw new Error(m)}
ok(/async function ensureOfficialFreeSurveyFallback\(\)/.test(f),'ensureOfficialFreeSurveyFallback missing');
ok(/exports\.getFeaturedHomeSurvey=onCall[\s\S]*ensureOfficialFreeSurveyFallback\(\)/.test(f),'getFeaturedHomeSurvey does not call fallback');
ok(!/if\(!acceptedCandidates\.length\)throw featuredInvalidError[\s\S]*ensureOfficialFreeSurveyFallback/.test(f),'getFeaturedHomeSurvey throws before fallback');
ok(/official_free_survey/.test(f),'official fallback id missing');
const home=(app.match(/function renderHome\(\)[\s\S]*?function officialPublicPricingPlans/)||[''])[0];
ok(!/survey_demo/.test(home),'home uses survey_demo');
ok(!/empresa-exemplo/i.test(home),'home uses empresa-exemplo');
ok(!/tokenHash=/.test(home),'home URL uses tokenHash');
ok((home.match(/getFeaturedHomeSurveyUrl\(\)/g)||[]).length===1,'renderHome may loop getFeaturedHomeSurvey');
console.log('validate-home-no-featured-crash: PASS');
