const fs=require('fs');
const s=fs.readFileSync('app.js','utf8');
for(const x of ['isPublicResultRoute','renderPublicResultFromRoute','ValoraRepository.loadPublicResult',"releasePublicUi('public_result_route')"]){
  if(!s.includes(x))throw new Error('rota pública sem '+x);
}
const route=s.slice(s.indexOf('async function renderPublicResultFromRoute'),s.indexOf('async function safeRenderResultById'));
if(/loginUser|repository\.login|signInWithPassword|signInWithEmailAndPassword/.test(route))throw new Error('rota pública usa Auth/login');
console.log('legacy public result no auth: PASS');
