(function(global){
  'use strict';
  const api=()=>global.ValoraApiClient;
  function tokenFrom(result){return result?.token||result?.accessToken||result?.jwt;}
  async function login(payload){const r=await api().post('/auth/login',payload);const t=tokenFrom(r);if(t)api().setToken(t);return r;}
  async function registerCompany(payload){const r=await api().post('/auth/register-company',payload);const t=tokenFrom(r);if(t)api().setToken(t);return r;}
  async function getMe(){return api().get('/me');}
  async function getPublicPlans(){return api().get('/plans/public');}
  function normalizeSurveyArgs(arg1,arg2){return typeof arg1==='object'?arg1:{surveyId:arg1,...(arg2||{})};}
  async function validatePublicSurvey(arg1,arg2){const {surveyId,token,org}=normalizeSurveyArgs(arg1,arg2);return api().post(`/public/surveys/${encodeURIComponent(surveyId)}/validate`,{token,org:org||''});}
  function normalizeResultPayload(payload){const result=payload?.result;if(result&&!result.rawScore){result.rawScore=result.totalScore||0;result.maxScore=result.maxScore||125;result.percentage=result.percentage||0;result.normalized5=result.maxScore?Math.round((result.rawScore/result.maxScore*5)*100)/100:0;result.level={label:result.maturityLabel||'Resultado',description:result.strategicTruth||'',recommendation:result.nextLevelRecommendation||''};result.byDimension=Object.fromEntries((payload.dimensions||[]).map(d=>[d.dimensionName||d.dimension_name,{score:d.score,maxScore:d.maxScore,percentage:d.percentage,normalized5:d.maxScore?Math.round((d.score/d.maxScore*5)*100)/100:0}]));}return payload;}
  async function submitPublicSurveyResponse(arg1,arg2){const payload=typeof arg1==='object'?arg1:{surveyId:arg1,...(arg2||{})};return normalizeResultPayload(await api().post(`/public/surveys/${encodeURIComponent(payload.surveyId)}/responses`,{token:payload.token,participant:payload.participant||{},answers:payload.answers||{},lgpdConsent:!!payload.lgpdConsent,communicationConsent:!!payload.communicationConsent}));}
  async function loadPublicResult(responseId,resultToken){return normalizeResultPayload(await api().post(`/public/results/${encodeURIComponent(responseId)}`,{resultToken}));}
  function downloadCertificatePdf(responseId){return `${api().getBaseUrl()}/responses/${encodeURIComponent(responseId)}/certificate.pdf`;}
  function downloadCertificatePng(responseId){return `${api().getBaseUrl()}/responses/${encodeURIComponent(responseId)}/certificate.png`;}
  async function getMigrationStatus(){return api().get('/admin/migration/status');}
  async function getApiHealth(){return api().get('/health');}
  async function getDatabaseHealth(){return api().get('/health/database');}
  async function getArchitectureStatus(){return api().get('/admin/architecture/status');}
  async function sendResultEmail(responseId,payload={}){return api().post(`/communications/result/${encodeURIComponent(responseId)}/send-email`,payload);}
  global.ValoraApiRepository=Object.freeze({login,registerCompany,getMe,getPublicPlans,publicPlans:getPublicPlans,validatePublicSurvey,submitPublicSurveyResponse,submitPublicSurvey:submitPublicSurveyResponse,loadPublicResult,loadResult:loadPublicResult,validateSurveyLink:validatePublicSurvey,downloadCertificatePdf,downloadCertificatePng,getMigrationStatus,getApiHealth,getDatabaseHealth,getArchitectureStatus,sendResultEmail});
})(window);
