const fs=require('fs');const app=fs.readFileSync('app.js','utf8'),f=fs.readFileSync('functions/index.js','utf8');
function ok(c,m){if(!c)throw new Error(m)}
ok(/callFirebaseFunction\('preparePublicSurveyLink',\{surveyId:survey\.id,featured:true,free:true\}\)/.test(app),'featured button must call preparePublicSurveyLink');
ok(/Pesquisa definida como destaque da home/.test(app),'success toast missing');
ok(/featuredHomeSurveyPromise/.test(app),'renderHome cache missing');
const home=(app.match(/function renderHome\(\)[\s\S]*?function officialPublicPricingPlans/)||[''])[0];ok((home.match(/getFeaturedHomeSurveyUrl\(\)/g)||[]).length===1,'possible featured loop');
ok(/exports\.getFeaturedHomeSurvey=onCall[\s\S]*diagnostics:\{totalSurveys/.test(f),'featured diagnostics missing');
console.log('validate-admin-featured-survey-flow: PASS');
