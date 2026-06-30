#!/usr/bin/env node
const fs=require('fs');
function read(f){return fs.readFileSync(f,'utf8')}
function ok(cond,msg){if(!cond){console.error(msg);process.exit(1)}}
const app=read('app.js'), api=read('api-repository.js'), noDemo=read('scripts/validate-no-demo-response-email-flow.js');
ok(/async function submitSurveyResponseLocally/.test(app),'submitSurveyResponseLocally ausente');
ok(/responseId:response\.id/.test(app),'retorno não usa responseId real');
ok(/resultToken:response\.resultToken/.test(app),'retorno não usa resultToken real');
ok(/dispatchPostSurveyCommunication\(response\.id\)/.test(app),'envio pós-resposta não usa id real');
ok(/communications\/result\/\$\{encodeURIComponent\(responseId\)\}\/send-email/.test(app),'endpoint oficial não encontrado no legado');
ok(/includeCertificate:true/.test(app),'payload não inclui certificado');
ok(/resultToken:response\?\.resultToken/.test(app),'payload não usa token real');
const blocked=[['resp','demo'].join('_'),['demo','response'].join('_'),['response','demo'].join('_')];
ok(!blocked.some(term=>app.includes(term)),'literal demonstrativo bloqueante em app.js');
ok(/sendResultEmail/.test(api),'api-repository sem sendResultEmail');
ok(/reports/.test(noDemo)&&/node_modules/.test(noDemo),'validador no-demo não atualizado');
console.log('validate-legacy-email-real-response-flow: PASS');
