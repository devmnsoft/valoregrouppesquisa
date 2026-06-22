const fs=require('fs');
const html=fs.readFileSync('app.js','utf8');
const css=fs.readFileSync('style.css','utf8');
const checks=[
 ['classes hero-premium existem', html.includes('hero-premium')&&css.includes('.hero-premium')],
 ['card diagnóstico alinhado', html.includes('hero-diagnostic-card')&&css.includes('grid-template-columns:1.15fr .85fr')],
 ['logo tem limite de largura', css.includes('max-width:220px')&&css.includes('max-width:160px')],
 ['pricing não tem texto técnico', !/console\.log|undefined|NaN/.test(html.match(/function renderPlans[\s\S]{0,4000}/)?.[0]||'')],
 ['cards não têm textos removidos', ['Responder','Receber leitura','Decidir','5 dimensões','25 perguntas','125 pontos'].every(t=>html.includes(t))]
];
let ok=true; for(const [name,pass] of checks){console.log(`${pass?'✅':'❌'} ${name}`); ok&&=pass;} if(!ok)process.exit(1);
