const fs=require('fs');
const app=fs.readFileSync('app.js','utf8');
const css=fs.readFileSync('style.css','utf8');
const checks=[
  ['renderHome resolves featured survey',/const featuredSurvey=resolveFeaturedFreeSurvey\(state\)/.test(app)],
  ['renderHome resolves featured link',/const featuredSurveyLink=resolveFeaturedSurveyLink\(featuredSurvey\)/.test(app)],
  ['no uninitialized surveyLink in renderHome',!/surveyLink/.test(app.slice(app.indexOf('function renderHome'),app.indexOf('function officialPublicPricingPlans')))],
  ['result panel contrast tokens',/--hero-panel-bg:#0b4b5c/.test(css)&&/--hero-panel-text:#f7fcfd/.test(css)],
  ['explicit dark panel text color',/\.hero-result-panel,\.hero-result-panel h2,\.hero-result-panel h3,\.hero-result-panel strong\{color:var\(--hero-panel-text\)\}/.test(css)],
  ['old v2 hero css removed',!/\.home-hero-v2\{/.test(css)],
  ['free CTA fallback exists',/Conhecer o plano Grátis/.test(app)],
  ['result preview exists',/Exemplo de devolutiva/.test(app)&&/Principal bloqueio/.test(app)]
];
const failed=checks.filter(([,ok])=>!ok);
for(const [name,ok] of checks)console.log(`${ok?'PASS':'FAIL'} ${name}`);
if(failed.length){process.exitCode=1;}
