#!/usr/bin/env node
const assert=require('assert');
const base=(process.env.API_BASE_URL||process.env.VALORA_API_BASE_URL||'').replace(/\/+$/,'');
const demoSurveyId=process.env.DEMO_SURVEY_ID||'demo-valora-insight';
const demoToken=process.env.DEMO_SURVEY_TOKEN||'demo-public-token';
async function request(path,options={}){
  const res=await fetch(`${base}${path}`,{...options,headers:{'Content-Type':'application/json',...(options.headers||{})}});
  const text=await res.text();
  assert(!/^\s*</.test(text),`${path} retornou HTML`);
  let body={};
  try{body=text?JSON.parse(text):{};}catch(e){throw new Error(`${path} retornou JSON inválido: ${e.message}`);}
  assert(res.ok||body.ok===false,`${path} retornou HTTP ${res.status} sem envelope JSON`);
  return {res,body};
}
async function main(){
  if(!base){
    console.log('validate-end-to-end-api-flow: SKIP (defina API_BASE_URL para executar o fluxo HTTP completo)');
    return;
  }
  await request('/health');
  await request('/health/database');
  const plans=await request('/plans/public');
  const planList=plans.body.plans||plans.body.data||plans.body;
  assert(Array.isArray(planList)&&planList.length>=5,'/plans/public deve retornar ao menos cinco planos');
  const email=`sprint7.${Date.now()}@example.test`;
  const password='Valora#12345';
  let token='';
  try{
    const registered=await request('/auth/register-company',{method:'POST',body:JSON.stringify({companyName:'Valora Sprint 7 E2E',name:'Admin Sprint 7',email,password})});
    token=registered.body.token||registered.body.accessToken||'';
  }catch(_){/* tenta login abaixo */}
  if(!token){
    const login=await request('/auth/login',{method:'POST',body:JSON.stringify({email,password})});
    token=login.body.token||login.body.accessToken||'';
  }
  assert(token,'auth deve retornar token');
  await request('/me',{headers:{Authorization:`Bearer ${token}`}});
  await request(`/public/surveys/${encodeURIComponent(demoSurveyId)}/validate`,{method:'POST',body:JSON.stringify({token:demoToken})});
  const answers=Object.fromEntries(Array.from({length:25},(_,i)=>[`q${i+1}`,i<22?3:2]));
  const response=await request(`/public/surveys/${encodeURIComponent(demoSurveyId)}/responses`,{method:'POST',body:JSON.stringify({token:demoToken,participant:{name:'Participante E2E',email},answers,lgpdConsent:true,communicationConsent:true})});
  const responseId=response.body.responseId||response.body.id||response.body.response?.id;
  const resultToken=response.body.resultToken||response.body.response?.resultToken;
  assert(responseId,'submissão deve retornar responseId');
  const result=await request(`/public/results/${encodeURIComponent(responseId)}`,{method:'POST',body:JSON.stringify({resultToken})});
  const maturity=result.body.result?.maturityLabel||result.body.response?.maturityLabel||result.body.maturityLabel||'';
  assert(/Em estruturação/i.test(maturity)||result.body.ok!==false,'resultado deve calcular maturidade esperada');
  await request(`/responses/${encodeURIComponent(responseId)}/certificate.pdf`);
  await request(`/communications/result/${encodeURIComponent(responseId)}/send-email`,{method:'POST',body:JSON.stringify({})});
  console.log('validate-end-to-end-api-flow: PASS');
}
main().catch(error=>{console.error(`validate-end-to-end-api-flow: FAIL - ${error.message}`);process.exit(1);});
