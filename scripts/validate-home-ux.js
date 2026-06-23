#!/usr/bin/env node
const fs=require('fs');
const path=require('path');
const root=path.resolve(__dirname,'..');
const files=['app.js','style.css','config.js','log-service.js'];
const text=Object.fromEntries(files.map(f=>[f,fs.readFileSync(path.join(root,f),'utf8')]));
const all=Object.values(text).join('\n');
const checks=[];
function check(name,ok,details=''){checks.push({name,ok,details});}
['home-hero-v2','home-hero-inner','hero-brand-line','hero-brand-logo','hero-eyebrow','hero-proof-row','hero-insight-panel','panel-metrics','panel-steps','free-diagnostic-strip'].forEach(c=>check(`Classe ${c}`,all.includes(c),`${c} encontrada`));
check('surveyLink definido antes do template',/function renderHome\(\)\{[\s\S]*const featuredSurvey=getFeaturedFreeSurvey\(\);[\s\S]*const surveyLink=buildPublicSurveyUrl\(featuredSurvey\);[\s\S]*\$\('#app'\)\.innerHTML=`/.test(text['app.js']));
check('Sem surveyLink indefinido',!/featured\?surveyLink\(|[^A-Za-z0-9_]surveyLink\(/.test(text['app.js']),'renderHome usa buildPublicSurveyUrl e variável local segura.');
check('Função getFeaturedFreeSurvey',/function\s+getFeaturedFreeSurvey\s*\(/.test(text['app.js']));
check('Função buildPublicSurveyUrl',/function\s+buildPublicSurveyUrl\s*\(/.test(text['app.js']));
check('Cloud Functions desabilitadas',/ENABLE_CLOUD_FUNCTIONS\s*:\s*false/.test(text['config.js']));
check('persistRemote sai antes no Spark',/cfg\.ENABLE_CLOUD_FUNCTIONS!==true\)return false/.test(text['log-service.js']));
check('createSystemLog não chama remoto no Spark',/ENABLE_CLOUD_FUNCTIONS===true&&window\.ValoraConfig\?\.STORAGE_MODE==='firebase'\)persistRemote\(log\)/.test(text['log-service.js']));
['Diagnóstico gratuito','Responder diagnóstico grátis','Valora Insight™','5 dimensões','25 perguntas','125 pontos'].forEach(t=>check(`Texto público ${t}`,all.includes(t)));
const homeSegment=text['app.js'].match(/function renderHome\(\)[\s\S]*?function officialPublicPricingPlans/)?.[0]||'';
['Blaze','Cloud Functions','Firebase'].forEach(t=>check(`Home pública sem ${t}`,!homeSegment.includes(t)));
check('Home usa âncora pública para CTA gratuito',/<a class="btn btn-primary" href="\$\{esc\(surveyLink\)\}">Responder diagnóstico grátis<\/a>/.test(homeSegment));
check('Sem logo gigante isolada no hero novo',!homeSegment.includes('home-hero-panel')&&!homeSegment.includes('time-ring'));
const failed=checks.filter(c=>!c.ok);
for(const c of checks)console.log(`${c.ok?'PASS':'FAIL'} ${c.name}${c.details?` - ${c.details}`:''}`);
if(failed.length){console.error(`\n${failed.length} validação(ões) falharam.`);process.exit(1);}console.log('\nHome UX validada com sucesso.');
