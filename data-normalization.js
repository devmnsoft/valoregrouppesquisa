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
  {id:'faq_valora_insight',question:'O que é o Valora Insight™?',answer:'É um diagnóstico de maturidade organizacional que ajuda a identificar forças, fragilidades e prioridades de evolução da empresa.'},
  {id:'faq_tempo',question:'Quanto tempo leva para responder?',answer:'O diagnóstico essencial foi pensado para ser rápido e objetivo, levando poucos minutos para ser respondido.'},
  {id:'faq_resultado',question:'O que recebo ao final?',answer:'Você recebe uma leitura de maturidade com pontuação, nível, principais dimensões analisadas e uma devolutiva estratégica conforme o plano contratado.'},
  {id:'faq_gratis',question:'Existe um diagnóstico gratuito?',answer:'Sim. O plano Grátis permite iniciar com uma pesquisa ativa, até 10 respostas, resultado individual, devolutiva resumida e certificado simples.'},
  {id:'faq_empresa',question:'A plataforma serve para qualquer empresa?',answer:'Sim. O Valora Pulse™ pode ser usado por empresas em diferentes estágios para organizar diagnósticos, respostas, relatórios e planos de evolução.'}
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
const FAQ_META_KEYS=new Set(['id','uid','createdAt','updatedAt','updatedBy','createdBy','migratedAt','migratedBy','source','version','storeKey','metadata','settings','email','contactEmail','whatsappNumber','whatsappEnabled','featuredSurveyId','inviteSubject','inviteBody','resultSubject','resultBody','lgpdText']);
function isFaqLikeItem(item){return !!(item&&typeof item==='object'&&!Array.isArray(item)&&(item.question||item.pergunta||item.title||item.titulo||item.answer||item.resposta||item.content||item.texto));}
function normalizeFaqItem(item,index){
  if(typeof item==='string')return {id:`faq_${index+1}`,question:item.trim(),answer:''};
  if(!isFaqLikeItem(item))return null;
  const question=item.question||item.pergunta||item.title||item.titulo||'';
  const answer=item.answer||item.resposta||item.content||item.texto||'';
  const q=String(question||'').trim(),a=String(answer||'').trim();
  if(!q&&!a)return null;
  return {id:item.id||`faq_${index+1}`,question:q,answer:a};
}
function normalizeFaqItems(value,fallback=defaultFaq()){
  let raw=value;
  if(typeof raw==='string')raw=parseFaq(raw);
  if(Array.isArray(raw)){
    const normalized=raw.map(normalizeFaqItem).filter(Boolean);
    return normalized.length?normalized:fallback;
  }
  if(raw&&typeof raw==='object'){
    if(Array.isArray(raw.items))return normalizeFaqItems(raw.items,fallback);
    if(Array.isArray(raw.faq))return normalizeFaqItems(raw.faq,fallback);
    if(Array.isArray(raw.questions))return normalizeFaqItems(raw.questions,fallback);
    const numericKeys=Object.keys(raw).filter(key=>/^\d+$/.test(key)).sort((a,b)=>Number(a)-Number(b));
    if(numericKeys.length){
      const normalized=numericKeys.map(key=>normalizeFaqItem(raw[key],Number(key))).filter(Boolean);
      return normalized.length?normalized:fallback;
    }
    const possibleFaqPairs=Object.entries(raw).filter(([key,answer])=>{
      if(FAQ_META_KEYS.has(key)||!answer||typeof answer!=='string')return false;
      const lower=key.toLowerCase();
      return key.includes('?')||key.length>12||lower.startsWith('o que')||lower.startsWith('como')||lower.startsWith('qual')||lower.startsWith('existe');
    }).map(([question,answer],index)=>({id:`faq_pair_${index+1}`,question,answer}));
    if(possibleFaqPairs.length)return possibleFaqPairs;
  }
  return fallback;
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
