#!/usr/bin/env node
const fs=require('fs');
const fail=[];const ok=(cond,msg)=>{if(!cond)fail.push(msg)};
const read=f=>fs.existsSync(f)?fs.readFileSync(f,'utf8'):'';
const index=read('index.html'),cfg=read('config.js')+read('config/config.local-api.js')+read('config/config.hybrid.js'),api=read('api-client.js'),repo=read('api-repository.js');
ok(fs.existsSync('api-client.js'),'api-client.js não existe.');
ok(fs.existsSync('api-repository.js'),'api-repository.js não existe.');
ok(index.indexOf('api-client.js')>-1&&index.indexOf('api-repository.js')>-1&&index.indexOf('api-client.js')<index.indexOf('api-repository.js'),'index.html deve carregar api-client antes de api-repository.');
ok(/DATA_PROVIDER:\s*'firebase'/.test(cfg)&&/DATA_PROVIDER:\s*'api'/.test(cfg)&&/DATA_PROVIDER:\s*'hybrid'/.test(cfg),'DATA_PROVIDER deve suportar firebase/api/hybrid.');
ok(/API_BASE_URL/.test(api)&&/sessionStorage/.test(api)&&/A API retornou formato inesperado/.test(api),'api-client não implementa regras de API_BASE_URL/sessionStorage/formato.');
ok(/validatePublicSurvey/.test(repo)&&/submitPublicSurveyResponse/.test(repo)&&/getMigrationStatus/.test(repo),'api-repository sem métodos obrigatórios.');
if(fail.length){console.error(fail.join('\n'));process.exit(1);}console.log('validate-api-provider: PASS');
