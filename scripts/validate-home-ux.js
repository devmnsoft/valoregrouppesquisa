#!/usr/bin/env node
const fs=require('fs');
const path=require('path');
const root=path.resolve(__dirname,'..');
const files=['app.js','style.css','config.js','log-service.js'];
const text=Object.fromEntries(files.map(f=>[f,fs.readFileSync(path.join(root,f),'utf8')]));
const all=Object.values(text).join('\n');
const checks=[];
function check(name,ok,details=''){checks.push({name,ok,details});}
['home-hero','home-hero-grid','home-hero-copy','home-hero-panel','hero-logo','hero-meta','hero-actions','diagnostic-summary','time-ring','free-diagnostic-card','journey-steps'].forEach(c=>check(`Classe ${c}`,all.includes(c),`${c} encontrada`));
check('Sem surveyLink indefinido',!/featured\?surveyLink\(|[^A-Za-z0-9_]surveyLink\(/.test(text['app.js']),'renderHome usa buildPublicSurveyUrl e variável local segura.');
check('Função getFeaturedFreeSurvey',/function\s+getFeaturedFreeSurvey\s*\(/.test(text['app.js']));
check('Função buildPublicSurveyUrl',/function\s+buildPublicSurveyUrl\s*\(/.test(text['app.js']));
check('Cloud Functions desabilitadas',/ENABLE_CLOUD_FUNCTIONS\s*:\s*false/.test(text['config.js']));
check('persistRemote sai antes no Spark',/cfg\.ENABLE_CLOUD_FUNCTIONS!==true\)return false/.test(text['log-service.js']));
['Diagnóstico gratuito','Responder diagnóstico grátis','Grátis','Essencial','Profissional','Corporativo','Enterprise'].forEach(t=>check(`Texto público ${t}`,all.includes(t)));
['Integrações sob demanda quando migrar para Blaze','Exportação CSV/JSON','ValoraBot básico'].forEach(t=>check(`Texto proibido ausente: ${t}`,!text['app.js'].includes(t)));
const homeSegment=text['app.js'].match(/function renderHome\(\)[\s\S]*?function officialPublicPricingPlans/)?.[0]||'';
check('Home pública sem Blaze',!homeSegment.includes('Blaze'));
check('Home pública sem Cloud Functions',!/Cloud Functions/.test(homeSegment));
const failed=checks.filter(c=>!c.ok);
for(const c of checks)console.log(`${c.ok?'PASS':'FAIL'} ${c.name}${c.details?` - ${c.details}`:''}`);
if(failed.length){console.error(`\n${failed.length} validação(ões) falharam.`);process.exit(1);}console.log('\nHome UX validada com sucesso.');
