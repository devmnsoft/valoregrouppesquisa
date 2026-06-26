#!/usr/bin/env node
const fs=require('fs');const fail=[];const app=fs.readFileSync('app.js','utf8');const cfg=fs.readFileSync('config.js','utf8');
function ok(c,m){if(!c)fail.push(m)}
ok(/DATA_PROVIDER\s*:\s*'firebase'/.test(cfg),'DATA_PROVIDER default deve continuar firebase.');
ok(/HYBRID_PRIMARY_PROVIDER\s*:\s*'(firebase|api)'/.test(cfg),'HYBRID_PRIMARY_PROVIDER ausente.');
ok(/DATA_PROVIDER/.test(app)&&/hybrid/.test(app),'app.js não contém roteamento hybrid.');
ok(/validatePublicSurveyLink/.test(app)&&/submitPublicSurveyResponse/.test(app)&&/loadPublicResult/.test(app),'funções públicas únicas ausentes.');
if(fail.length){console.error(fail.join('\n'));process.exit(1)}console.log('validate-hybrid-provider: PASS');
