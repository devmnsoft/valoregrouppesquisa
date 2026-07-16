const {readApp,readCss,fail,functionBlock}=require('./validate-legacy-home-utils');
const app=readApp(), css=readCss();
const sec=functionBlock(app,'renderHowItWorksSection');
if(!sec) fail('renderHowItWorksSection ausente.');
['id="como-funciona"','Como funciona','1. Responda','2. Receba','3. Entenda','4. Evolua'].forEach(t=>{if(!sec.includes(t)) fail(`Como funciona sem: ${t}`);});
if(sec.includes('Diagnóstico gratuito Valora Insight™')||sec.includes('Diagnóstico Valora Insight™')) fail('Como funciona repete título/card de diagnóstico.');
['.how-it-works-section','.how-it-works-inner','.how-it-works-grid','@media (max-width: 900px)','@media (max-width: 560px)'].forEach(t=>{if(!css.includes(t)) fail(`CSS Como funciona ausente: ${t}`);});
console.log('validate-legacy-how-it-works-section: PASS');
