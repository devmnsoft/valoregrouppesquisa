(function(root){
'use strict';
function fallbackArray(fallback){return Array.isArray(fallback)?fallback:[];}
function asArray(value,fallback=[]){
  if(Array.isArray(value))return value;
  if(value===null||value===undefined||value==='')return fallbackArray(fallback);
  if(Array.isArray(value?.items))return value.items;
  if(Array.isArray(value?.list))return value.list;
  if(Array.isArray(value?.data))return value.data;
  if(Array.isArray(value?.questions))return value.questions;
  if(Array.isArray(value?.entries))return value.entries;
  if(typeof value==='object')return Object.values(value);
  return fallbackArray(fallback);
}
function asObject(value,fallback={}){return value&&typeof value==='object'&&!Array.isArray(value)?value:{...fallback};}
function asString(value,fallback=''){if(value===null||value===undefined)return fallback;if(typeof value==='object')return fallback;return String(value);}
function asNumber(value,fallback=0){const n=Number(value);return Number.isFinite(n)?n:fallback;}
function asBoolean(value,fallback=false){if(typeof value==='boolean')return value;if(value==='true')return true;if(value==='false')return false;return fallback;}
function defaultFaq(){return [
  {id:'faq_1',question:'O que é o Valora Insight™?',answer:'É um diagnóstico de maturidade organizacional que transforma respostas em uma leitura executiva sobre cultura, governança, liderança, pessoas e crescimento.'},
  {id:'faq_2',question:'Quanto tempo leva para responder?',answer:'O diagnóstico essencial leva poucos minutos e apresenta uma leitura resumida ao final.'},
  {id:'faq_3',question:'Existe plano gratuito?',answer:'Sim. O plano Grátis permite iniciar com uma pesquisa ativa, até 10 respostas e uma devolutiva resumida.'}
];}
function parseFaq(text){
  const lines=String(text||'').split(/\r?\n/).map(x=>x.trim()).filter(Boolean);
  const rows=[];
  for(let i=0;i<lines.length;i++){
    const line=lines[i], pipe=line.indexOf('|');
    if(pipe>0){rows.push({question:line.slice(0,pipe).trim(),answer:line.slice(pipe+1).trim()});continue;}
    if(line.includes('?')){const [q,...rest]=line.split('?');rows.push({question:`${q.trim()}?`,answer:rest.join('?').trim()});continue;}
    rows.push({question:line,answer:lines[i+1]&&!lines[i+1].includes('|')?lines[++i]:''});
  }
  return rows.length?rows:defaultFaq();
}
function normalizeFaqItems(value,fallback=defaultFaq()){
  let raw=value;
  if(typeof raw==='string')raw=parseFaq(raw);
  if(raw&&typeof raw==='object'&&!Array.isArray(raw)){
    if(Array.isArray(raw.items))raw=raw.items;else if(Array.isArray(raw.faq))raw=raw.faq;else if(Array.isArray(raw.questions))raw=raw.questions;else raw=Object.entries(raw).map(([question,answer])=>({question,answer}));
  }
  const arr=Array.isArray(raw)?raw:fallbackArray(fallback);
  const normalized=arr.map((item,index)=>{
    if(typeof item==='string')return {id:`faq_${index+1}`,question:item,answer:''};
    const obj=asObject(item);
    return {id:asString(obj.id,`faq_${index+1}`),question:asString(obj.question||obj.pergunta||obj.title||obj.titulo||obj.q,''),answer:asString(obj.answer||obj.resposta||obj.content||obj.texto||obj.a,'')};
  }).filter(item=>item.question||item.answer);
  return normalized.length?normalized:normalizeFaqItems(fallback,[]);
}
function normalizeEmailSettings(email={}){const e=asObject(email);return {...e,mode:asString(e.mode,'disabled'),senderName:asString(e.senderName,'Valora Group'),senderEmail:asString(e.senderEmail,'valoragroup@mnsoft.com.br')};}
function normalizeSettings(settings={}){const s=asObject(settings);return {...s,featuredSurveyId:asString(s.featuredSurveyId,''),contactEmail:asString(s.contactEmail,''),whatsappEnabled:asBoolean(s.whatsappEnabled,true),whatsappNumber:asString(s.whatsappNumber,'+55 91 99254-5353'),inviteSubject:asString(s.inviteSubject,'Convite para responder o diagnóstico'),inviteBody:asString(s.inviteBody,'Olá, você foi convidado para responder um diagnóstico de maturidade organizacional.'),resultSubject:asString(s.resultSubject,'Seu resultado Valora Insight™'),resultBody:asString(s.resultBody,'Seu resultado está disponível para consulta.'),lgpdText:asString(s.lgpdText,''),faq:normalizeFaqItems(s.faq,defaultFaq()),email:normalizeEmailSettings(s.email)};}
function normalizePlanSafe(plan={}){const p=asObject(plan);return {...p,id:asString(p.id,''),name:asString(p.name||p.title,''),features:asArray(p.features).map(x=>asString(x)).filter(Boolean),enabledModules:asArray(p.enabledModules)};}
function normalizeOrganizationSafe(org={}){const o=asObject(org);return {...o,id:asString(o.id||o.uid,''),name:asString(o.name||o.publicName||o.legalName,'Empresa sem nome'),publicName:asString(o.publicName||o.name||o.legalName,'Empresa sem nome'),settings:asObject(o.settings)};}
function normalizeFormSafe(form={}){const f=asObject(form);return {...f,dimensions:asArray(f.dimensions),questions:asArray(f.questions),resultBands:asArray(f.resultBands)};}
function normalizeSurveySafe(survey={}){const s=asObject(survey);return {...s,questions:asArray(s.questions),status:asString(s.status,'draft')};}
function normalizeResponseSafe(response={}){const r=asObject(response),participant=asObject(r.participant);return {...r,participant:{...participant,name:asString(participant.name,'Participante'),email:asString(participant.email,'')},answers:Array.isArray(r.answers)?r.answers:asObject(r.answers),normalized5:asNumber(r.normalized5??r.average,0),percentage:asNumber(r.percentage,0),rawScore:asNumber(r.rawScore??r.totalScore,0),maxScore:asNumber(r.maxScore??r.totalMax,0)};}
function normalizeAppState(rawState={}){const raw=asObject(rawState);const normalized={...raw};normalized.settings=normalizeSettings(raw.settings);['modules','plans','companies','organizations','users','forms','surveys','responses','invitations','invoices','actionPlans','notifications','knowledgeBase','supportCategories','supportSlaPolicies','supportTickets','supportMessages','integrations','webhooks','apiKeys','logs','integrationLogs','chatbotConversations','chatbotUnansweredQuestions'].forEach(k=>{normalized[k]=asArray(raw[k]);});normalized.plans=normalized.plans.map(normalizePlanSafe);normalized.companies=normalized.companies.map(normalizeOrganizationSafe);normalized.organizations=normalized.organizations.map(normalizeOrganizationSafe);normalized.forms=normalized.forms.map(normalizeFormSafe);normalized.surveys=normalized.surveys.map(normalizeSurveySafe);normalized.responses=normalized.responses.map(normalizeResponseSafe);return normalized;}
const api={asArray,asObject,asString,asNumber,asBoolean,defaultFaq,parseFaq,normalizeFaqItems,normalizeEmailSettings,normalizeSettings,normalizePlanSafe,normalizeOrganizationSafe,normalizeFormSafe,normalizeSurveySafe,normalizeResponseSafe,normalizeAppState};
if(typeof module!=='undefined'&&module.exports)module.exports=api;
root.ValoraDataNormalization=api;
})(typeof window!=='undefined'?window:globalThis);
