const {readApp,fail,count,functionBlock}=require('./validate-legacy-home-utils');
const app=readApp();
const hero=functionBlock(app,'renderFreeDiagnosticHero');
const home=functionBlock(app,'renderHome');
if(!/function\s+renderFreeDiagnosticHero\s*\(/.test(app)) fail('renderFreeDiagnosticHero ausente.');
['Diagnóstico gratuito','Valora Insight™','5 minutos','25 perguntas','devolutiva estratégica','Resultado no e-mail','Leitura resumida','Próximo passo claro','Diagnóstico Valora Insight™'].forEach(t=>{if(!hero.includes(t)) fail(`Hero principal sem: ${t}`);});
if(count(home,'renderFreeDiagnosticHero()')!==1) fail('Home deve usar uma única fonte de hero diagnóstica.');
if(/<section class="home-hero-v3"/.test(home)) fail('Home ainda renderiza hero antiga além da hero Valora Insight.');
console.log('validate-legacy-home-hero-single-source: PASS');
