const {readApp,fail,count,functionBlock}=require('./validate-legacy-home-utils');
const app=readApp();
const html=functionBlock(app,'renderFreeDiagnosticHero')+'\n'+functionBlock(app,'renderHowItWorksSection')+'\n'+functionBlock(app,'renderHome');
const limits={
  'Diagnóstico gratuito Valora Insight™':1,
  'Diagnóstico Valora Insight™':1,
  'Receba seu resultado no e-mail':1,
  'Acesse uma devolutiva resumida':1,
  'Fale com o Valora Group para conhecer os planos completos':1,
};
for(const [text,max] of Object.entries(limits)){const n=count(html,text);if(n>max)fail(`Texto duplicado (${n}/${max}): ${text}`);}
const heroCta=count(functionBlock(app,'renderFreeDiagnosticHero'),'Responder diagnóstico gratuito');
const initialHome=functionBlock(app,'renderHome').split('featuredSurveyUrlPromise.then')[0];
const cta=heroCta+count(initialHome,'Responder diagnóstico gratuito');
if(cta>2) fail(`Responder diagnóstico gratuito aparece mais de 2 vezes na renderização inicial da home (${cta}).`);
console.log('validate-legacy-home-free-diagnostic-occurrences: PASS');
