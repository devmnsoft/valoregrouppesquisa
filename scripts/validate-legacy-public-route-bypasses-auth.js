const fs=require('fs');const s=fs.readFileSync('app.js','utf8');const fail=[];
for(const k of ['function getPublicRouteParams','function isAnyPublicTokenRoute','releasePublicUi(\'public_token_route\'','state.user=null','return renderTakeSurveyFromRoute()','return renderPublicResultFromRoute()','renderIncompletePublicResultLink']) if(!s.includes(k)) fail.push(`ausente: ${k}`);
const init=s.slice(s.indexOf('async function init()'),s.indexOf("if(repository.mode==='firebase'",s.indexOf('async function init()')));
if(!/isAnyPublicTokenRoute\(\)[\s\S]*state\.user=null[\s\S]*renderPublicResultFromRoute/.test(init)) fail.push('rotas públicas por token não retornam antes do Firebase Auth/loadProfile');
if(/isAnyPublicTokenRoute\(\)[\s\S]{0,700}(loadProfile|renderLogin|signInWithPassword|signInWithEmailAndPassword)/.test(init)) fail.push('rota pública referencia auth/login no bloco de bypass');
if(fail.length){console.error(fail.join('\n'));process.exit(1)}console.log('validate-legacy-public-route-bypasses-auth: PASS');
