const {readApp,fail,count,functionBlock}=require('./validate-legacy-home-utils');
const app=readApp();
const home=functionBlock(app,'renderHome');
if(!home) fail('renderHome não encontrada.');
if(count(home,'renderFreeDiagnosticHero()')!==1) fail('Home deve renderizar renderFreeDiagnosticHero() exatamente uma vez.');
['renderFreeDiagnosticSection()','renderFreeDiagnosticMobileCard()','renderOfficialFreeSurvey()','renderHomeFreeSurvey()','renderFeaturedHomeSurvey()'].forEach(call=>{if(home.includes(call))fail(`Home ainda chama seção diagnóstica duplicada: ${call}`);});
if(home.includes('free-diagnostic-strip')) fail('Home ainda renderiza free-diagnostic-strip repetitiva.');
console.log('validate-legacy-home-no-duplicate-free-diagnostic: PASS');
