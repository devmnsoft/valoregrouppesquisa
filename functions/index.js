'use strict';
const admin=require('firebase-admin');
const {onCall,HttpsError}=require('firebase-functions/v2/https');
const {onSchedule}=require('firebase-functions/v2/scheduler');
const {defineSecret}=require('firebase-functions/params');
const nodemailer=require('nodemailer');
const {sha256,timingSafeEqualHex,createToken}=require('./utils/hash');
const {required,asObject,asBoolean,validateParticipant}=require('./utils/validation');
const {auditLog}=require('./utils/audit');
admin.initializeApp();
const db=admin.firestore();
const SMTP_PASSWORD=defineSecret('SMTP_PASSWORD');
const TS=admin.firestore.FieldValue.serverTimestamp;

const EMAIL_RE=/^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const INVITE_STATUSES=new Set(['pending','sent','opened','answered','expired','failed','resent','cancelled']);
const ALLOWED_TEMPLATES=new Set(['invite','result','test','operational']);
function cleanText(v,max=5000){return String(v||'').replace(/[\u0000-\u001f\u007f]/g,' ').trim().slice(0,max);}
function onlyDigits(v){return String(v||'').replace(/\D/g,'');}
function maskEmail(email){const [name,domain]=String(email||'').split('@');if(!domain)return '';return `${name.slice(0,2)}***@${domain}`;}
async function authedUser(req){
  if(!req.auth?.uid)throw new HttpsError('unauthenticated','Entre novamente para continuar.');
  const snap=await db.collection('users').doc(req.auth.uid).get();
  const claims=req.auth.token||{}, d=snap.exists?snap.data():{};
  const role=claims.role||d.role, companyId=claims.companyId||d.companyId||'';
  if(d.status&&d.status!=='active')throw new HttpsError('permission-denied','Usuário inativo.');
  return {uid:req.auth.uid,email:claims.email||d.email||'',role,companyId,name:d.name||claims.name||''};
}
async function assertSurveyCompany(surveyId,user){
  if(!surveyId)return null;
  const snap=await db.collection('surveys').doc(surveyId).get();
  if(!snap.exists)throw new HttpsError('not-found','Pesquisa não encontrada.');
  const s={id:snap.id,...snap.data()};
  if(user.role!=='admin_valora'&&s.companyId!==user.companyId)throw new HttpsError('permission-denied','Pesquisa fora da sua empresa.');
  return s;
}
function publicEmailConfig(){return {configured:!!(process.env.SMTP_HOST&&process.env.SMTP_USERNAME&&process.env.SMTP_SENDER_EMAIL),senderName:process.env.SMTP_SENDER_NAME||'Valora Group',senderEmail:maskEmail(process.env.SMTP_SENDER_EMAIL||process.env.SMTP_USERNAME||'')};}
function buildEmailHtml(templateType,payload){const title=cleanText(payload.title||payload.subject||'Valora Pulse™',140),message=cleanText(payload.message||payload.text||'',4000),buttonUrl=cleanText(payload.buttonUrl||payload.link||'',800),buttonLabel=cleanText(payload.buttonLabel||'Acessar',40);return `<!doctype html><html><body style="font-family:Arial,sans-serif;color:#082a37;background:#eef7f9;padding:24px"><main style="max-width:680px;margin:auto;background:white;border-radius:16px;padding:28px"><h1>${title}</h1><p style="white-space:pre-line;line-height:1.6">${message}</p>${buttonUrl?`<p><a href="${buttonUrl}" style="background:#0b3d4d;color:white;padding:12px 18px;border-radius:999px;text-decoration:none">${buttonLabel}</a></p>`:''}<small>Valora Group • ${templateType}</small></main></body></html>`;}
async function callJson(url,msg){const r=await fetch(url,{headers:{'user-agent':'ValoraPulse/1.0'}});const b=await r.json().catch(()=>null);if(!r.ok||!b)throw new Error(msg);return b;}

function ip(req){return req.rawRequest?.headers?.['x-forwarded-for']?.split(',')[0]?.trim()||req.rawRequest?.ip||'';}
function ua(req){return req.rawRequest?.headers?.['user-agent']||'';}
async function rateLimit(key,limit,windowMs){const ref=db.collection('rateLimits').doc(key);const now=Date.now();let allowed=false;await db.runTransaction(async tx=>{const s=await tx.get(ref);const d=s.exists?s.data():{};let count=d.resetAt?.toMillis?.()>now?Number(d.count||0):0;if(count>=limit)throw new HttpsError('resource-exhausted','Muitas tentativas. Aguarde e tente novamente.');count++;tx.set(ref,{count,resetAt:new Date(now+windowMs),updatedAt:TS()},{merge:true});allowed=true;});return allowed;}
function isBetween(s){const now=Date.now(),start=s.startsAt?.toMillis?.()||Date.parse(s.startsAt||0)||0,end=s.expiresAt?.toMillis?.()||Date.parse(s.expiresAt||0)||0;return (!start||start<=now)&&(!end||end>=now);}
async function loadValidSurvey({surveyId,token,req,action}){await rateLimit(`${action}:${ip(req)||'unknown'}:${surveyId}`,action==='submit'?10:60,15*60*1000);const snap=await db.collection('surveys').doc(surveyId).get();if(!snap.exists)throw new HttpsError('not-found','Pesquisa não encontrada.');const survey={id:snap.id,...snap.data()};if(survey.status!=='active'||survey.revokedAt||survey.revoked===true)throw new HttpsError('failed-precondition','Pesquisa indisponível.');if(!isBetween(survey))throw new HttpsError('failed-precondition','Pesquisa fora do período de validade.');if(!survey.tokenHash||!timingSafeEqualHex(sha256(token),survey.tokenHash))throw new HttpsError('permission-denied','Token inválido.');const [orgSnap,legacyCompanySnap,formSnap]=await Promise.all([db.collection('organizations').doc(survey.companyId).get(),db.collection('companies').doc(survey.companyId).get(),db.collection('forms').doc(survey.formId).get()]);const companySnap=orgSnap.exists?orgSnap:legacyCompanySnap;if(!companySnap.exists||!['active','trial'].includes(companySnap.data().status))throw new HttpsError('failed-precondition','Empresa indisponível.');if(!formSnap.exists)throw new HttpsError('failed-precondition','Formulário indisponível.');const company={id:companySnap.id,...companySnap.data()},form={id:formSnap.id,...formSnap.data()};const plan=company.planId?(await db.collection('plans').doc(company.planId).get()).data():null;if(plan?.participationEnabled===false)throw new HttpsError('failed-precondition','Plano não permite participação.');if(Number.isFinite(survey.maxResponses)&&survey.responseCount>=survey.maxResponses)throw new HttpsError('resource-exhausted','Limite de respostas da pesquisa atingido.');if(Number(plan?.maxResponsesMonth)>=0){const month=new Date();month.setUTCDate(1);month.setUTCHours(0,0,0,0);const qs=await db.collection('responses').where('companyId','==',survey.companyId).where('createdAt','>=',month).count().get();if(qs.data().count>=Number(plan.maxResponsesMonth))throw new HttpsError('resource-exhausted','Limite mensal de respostas do plano atingido.');}
return {survey,company,form};}
function publicPayload({survey,company,form}){return {survey:{id:survey.id,title:survey.title,description:survey.description||'',companyId:survey.companyId,formId:survey.formId,requireIdentification:survey.requireIdentification!==false,lgpdRequired:survey.lgpdRequired!==false,allowRepeat:survey.allowRepeat===true,anonymous:survey.anonymous===true,showResult:survey.showResult!==false},company:{name:company.name},form:{id:form.id,name:form.name,description:form.description||'',timeMin:form.timeMin||5,scoringMethod:form.scoringMethod,dimensions:form.dimensions||[],questions:(form.questions||[]).map(q=>({id:q.id,text:q.text,help:q.help||'',dimensionId:q.dimensionId,dimensionName:q.dimensionName,type:q.type,required:q.required===true,options:(q.options||[]).map(o=>({id:o.id,text:o.text}))})),resultBands:form.resultBands||[]},lgpd:{text:form.lgpdText||survey.lgpdText||'',required:survey.lgpdRequired!==false},identification:{required:survey.requireIdentification!==false,anonymous:survey.anonymous===true}};}
function calc(form,answers){let rawScore=0,maxScore=0;const byDimension={},ratios=[],qualitative=[];for(const q of form.questions||[]){const a=answers[q.id];let raw=0,max=Number(q.maxScore||0);if(q.type==='scale'){const n=Math.max(0,Math.min(5,Number(a||0)));max=max||5;raw=n/5*max;}else if(q.type==='single'||q.type==='singleCorrect'){const o=(q.options||[]).find(x=>x.id===a);max=max||Math.max(1,...(q.options||[]).map(x=>Number(x.score||0)));raw=q.type==='singleCorrect'?(o?.correct?max:Number(o?.score||0)):Number(o?.score||0);}else if(q.type==='multiple'){const arr=Array.isArray(a)?a:(a?[a]:[]);raw=(q.options||[]).filter(o=>arr.includes(o.id)).reduce((s,o)=>s+Number(o.score||0),0);max=max||(q.options||[]).filter(o=>Number(o.score)>0).reduce((s,o)=>s+Number(o.score||0),0);}else{const text=String(a||'').trim();qualitative.push({question:q.text,answer:text});raw=text?Number(q.scoreWhenFilled||0):0;max=max||Number(q.scoreWhenFilled||0);}const weight=Math.max(0,Number(q.weight||1)),wr=raw*weight,wm=max*weight;rawScore+=wr;maxScore+=wm;ratios.push({ratio:max>0?raw/max:0,weight});const dim=q.dimensionName||'Geral';byDimension[dim]??={rawScore:0,maxScore:0,count:0};byDimension[dim].rawScore+=wr;byDimension[dim].maxScore+=wm;byDimension[dim].count++;}let normalized5=maxScore?rawScore/maxScore*5:0;if(form.scoringMethod==='weightedAverage'){const w=ratios.reduce((s,x)=>s+x.weight,0);normalized5=w?ratios.reduce((s,x)=>s+x.ratio*x.weight,0)/w*5:0;}normalized5=Math.max(0,Math.min(5,normalized5));Object.values(byDimension).forEach(d=>{d.normalized5=d.maxScore?Math.max(0,Math.min(5,d.rawScore/d.maxScore*5)):0;d.percentage=d.normalized5/5*100;});const level=(form.resultBands||[]).find(b=>normalized5>=Number(b.from)&&normalized5<=Number(b.to))||null;return {rawScore,maxScore,normalized5,percentage:normalized5/5*100,scoringMethod:form.scoringMethod,byDimension,level,qualitative};}
exports.validateSurveyLink=onCall(async req=>{const surveyId=required(req.data,'surveyId'),token=required(req.data,'token');const ctx=await loadValidSurvey({surveyId,token,req,action:'validate'});await auditLog(db,{action:'validateSurveyLink',actorType:'public',companyId:ctx.survey.companyId,entity:'survey',entityId:surveyId,ip:ip(req),userAgent:ua(req)});return publicPayload(ctx);});
exports.submitSurveyResponse=onCall(async req=>{const data=asObject(req.data,'payload'),surveyId=required(data,'surveyId'),token=required(data,'token');const ctx=await loadValidSurvey({surveyId,token,req,action:'submit'});const answers=asObject(data.answers,'answers'),participant=validateParticipant(data.participant||{});if(!participant.name||!participant.email)throw new HttpsError('invalid-argument','Nome e e-mail são obrigatórios.');if(ctx.survey.lgpdRequired!==false&&!asBoolean(data.lgpdConsent))throw new HttpsError('failed-precondition','Aceite LGPD obrigatório.');for(const q of ctx.form.questions||[]){const v=answers[q.id];if(q.required&&(v===undefined||v===null||v===''||(Array.isArray(v)&&!v.length)))throw new HttpsError('invalid-argument',`Pergunta obrigatória não respondida: ${q.id}`);}if(!ctx.survey.allowRepeat){const dup=await db.collection('responses').where('surveyId','==',surveyId).where('participant.email','==',participant.email).limit(1).get();if(!dup.empty)throw new HttpsError('already-exists','Participante já respondeu esta pesquisa.');}const invitationId=cleanText(data.invitationId||'',160),participantId=cleanText(data.participantId||'',160);const result=calc(ctx.form,answers),resultToken=createToken(24);const ref=db.collection('responses').doc();const response={surveyId,formId:ctx.form.id,companyId:ctx.survey.companyId,invitationId,participantId,department:participant.department||cleanText(data.department||'',160),participant:{...participant,lgpdConsent:asBoolean(data.lgpdConsent),communicationConsent:asBoolean(data.communicationConsent)},answers,lgpdConsent:{accepted:asBoolean(data.lgpdConsent),acceptedAt:TS(),textVersion:ctx.survey.lgpdVersion||ctx.form.lgpdVersion||'default'},communicationConsent:asBoolean(data.communicationConsent),ip:ip(req),userAgent:ua(req),resultTokenHash:sha256(resultToken),createdAt:TS(),...result};await db.runTransaction(async tx=>{tx.set(ref,response);tx.update(db.collection('surveys').doc(surveyId),{responseCount:admin.firestore.FieldValue.increment(1),updatedAt:TS()});if(invitationId)tx.set(db.collection('invitations').doc(invitationId),{status:'answered',answeredAt:TS(),updatedAt:TS()},{merge:true});else tx.set(db.collection('invitations').doc(`${surveyId}_${sha256(participant.email).slice(0,16)}`),{surveyId,companyId:ctx.survey.companyId,email:participant.email,status:'answered',answeredAt:TS(),updatedAt:TS()},{merge:true});});await auditLog(db,{action:'submitSurveyResponse',actorType:'public',companyId:ctx.survey.companyId,entity:'response',entityId:ref.id,after:{surveyId},ip:ip(req),userAgent:ua(req)});return {responseId:ref.id,resultToken,accessToken:resultToken,score:{rawScore:result.rawScore,maxScore:result.maxScore,normalized5:result.normalized5,percentage:result.percentage},level:result.level,message:result.level?.recommendation||'Resposta registrada com sucesso.'};});
exports.getPublicResult=onCall(async req=>{const responseId=required(req.data,'responseId'),resultToken=required(req.data,'resultToken');await rateLimit(`result:${ip(req)||'unknown'}:${responseId}`,60,15*60*1000);const snap=await db.collection('responses').doc(responseId).get();if(!snap.exists)throw new HttpsError('not-found','Resultado não encontrado.');const r={id:snap.id,...snap.data()};if(!r.resultTokenHash||!timingSafeEqualHex(sha256(resultToken),r.resultTokenHash))throw new HttpsError('permission-denied','Token de resultado inválido.');await auditLog(db,{action:'getPublicResult',actorType:'public',companyId:r.companyId,entity:'response',entityId:responseId,ip:ip(req),userAgent:ua(req)});return {id:r.id,surveyId:r.surveyId,companyId:r.companyId,participant:{name:r.participant?.name,email:r.participant?.email},createdAt:r.createdAt,rawScore:r.rawScore,maxScore:r.maxScore,normalized5:r.normalized5,percentage:r.percentage,byDimension:r.byDimension,level:r.level,qualitative:r.qualitative||[]};});
exports.hashSurveyToken=onCall(async req=>({tokenHash:sha256(required(req.data,'token'))}));



async function monthlyCount(collection,companyId,field='createdAt'){
  const month=new Date();month.setUTCDate(1);month.setUTCHours(0,0,0,0);
  const qs=await db.collection(collection).where('companyId','==',companyId).where(field,'>=',month).count().get();
  return qs.data().count||0;
}
async function sendOneEmail({to,subject,templateType,payload}){
  const cfg=publicEmailConfig();if(!cfg.configured||!SMTP_PASSWORD.value())throw new HttpsError('failed-precondition','Envio de e-mail não configurado.');
  const transporter=nodemailer.createTransport({host:process.env.SMTP_HOST,port:Number(process.env.SMTP_PORT||587),secure:String(process.env.SMTP_SECURITY||'starttls')==='ssl',auth:{user:process.env.SMTP_USERNAME,pass:SMTP_PASSWORD.value()}});
  return transporter.sendMail({from:{name:process.env.SMTP_SENDER_NAME||'Valora Group',address:process.env.SMTP_SENDER_EMAIL||process.env.SMTP_USERNAME},to,subject,text:cleanText(payload.text||payload.message,4000),html:buildEmailHtml(templateType,payload)});
}
exports.sendSurveyInvitations=onCall({secrets:[SMTP_PASSWORD]},async req=>{
  const user=await authedUser(req), data=asObject(req.data,'payload');
  if(!['admin_valora','empresa_admin','gestor_pesquisa'].includes(user.role))throw new HttpsError('permission-denied','Seu perfil não pode enviar convites.');
  const survey=await assertSurveyCompany(required(data,'surveyId'),user);
  if(survey.status!=='active'||!isBetween(survey))throw new HttpsError('failed-precondition','Pesquisa inativa ou expirada.');
  const orgSnap=await db.collection('organizations').doc(survey.companyId).get(), legacyCompanySnap=await db.collection('companies').doc(survey.companyId).get();
  const companySnap=orgSnap.exists?orgSnap:legacyCompanySnap;
  if(!companySnap.exists||!['active','trial'].includes(companySnap.data().status))throw new HttpsError('failed-precondition','Empresa indisponível.');
  const plan=companySnap.data().planId?(await db.collection('plans').doc(companySnap.data().planId).get()).data():{};
  const recipients=Array.isArray(data.recipients)?data.recipients:[]; if(!recipients.length)throw new HttpsError('invalid-argument','Selecione ao menos um destinatário.');
  const used=await monthlyCount('invitations',survey.companyId,'createdAt'), limit=Number(plan?.maxEmailsMonth);
  if(Number.isFinite(limit)&&limit>=0&&used+recipients.length>limit)throw new HttpsError('resource-exhausted','Limite mensal de e-mails do plano atingido.');
  const out=[];
  for(const r0 of recipients){
    const r=asObject(r0,'recipient'), email=cleanText(r.email,254).toLowerCase(), name=cleanText(r.name,160);
    const invRef=db.collection('invitations').doc();
    const base={surveyId:survey.id,companyId:survey.companyId,participantId:cleanText(r.participantId||r.id||'',160),email,name,role:cleanText(r.role||'',80),department:cleanText(r.department||'',160),link:cleanText(r.link||'',1000),status:'pending',createdAt:TS(),updatedAt:TS(),expiredAt:survey.expiresAt||null};
    if(r.status==='inactive'||r.active===false||!EMAIL_RE.test(email)||r.receivesEmail===false){const failedReason=!EMAIL_RE.test(email)?'empty_or_invalid_email':r.receivesEmail===false?'receivesEmail_false':'inactive_participant';await invRef.set({...base,status:'failed',failedReason});out.push({id:invRef.id,email,status:'failed',failedReason});continue;}
    try{const info=await sendOneEmail({to:email,subject:cleanText(data.subject||`Convite: ${survey.title}`,140),templateType:'invite',payload:{message:cleanText(data.message||`Olá, ${name}. Acesse o link seguro para responder: ${base.link}`,4000),buttonUrl:base.link,buttonLabel:'Responder pesquisa'}});await invRef.set({...base,status:'sent',sentAt:TS(),messageId:info.messageId});out.push({id:invRef.id,email,status:'sent'});}catch(err){await invRef.set({...base,status:'failed',failedReason:cleanText(err.message,500)});out.push({id:invRef.id,email,status:'failed',failedReason:err.message});}
  }
  await auditLog(db,{action:'sendSurveyInvitations',actorId:user.uid,actorType:user.role,companyId:survey.companyId,entity:'survey',entityId:survey.id,after:{total:out.length,sent:out.filter(x=>x.status==='sent').length},ip:ip(req),userAgent:ua(req)});
  return {processed:out.length,sent:out.filter(x=>x.status==='sent').length,items:out};
});

exports.getEmailStatus=onCall({secrets:[SMTP_PASSWORD]},async req=>{await rateLimit(`emailStatus:${ip(req)||'unknown'}`,60,15*60*1000);return publicEmailConfig();});
exports.sendEmail=onCall({secrets:[SMTP_PASSWORD]},async req=>{
  const user=await authedUser(req), data=asObject(req.data,'payload');
  if(!['admin_valora','empresa_admin','gestor_pesquisa'].includes(user.role))throw new HttpsError('permission-denied','Seu perfil não pode enviar e-mails.');
  const to=cleanText(data.to,254).toLowerCase(), subject=cleanText(data.subject,140), templateType=cleanText(data.templateType||'operational',40);
  if(!EMAIL_RE.test(to))throw new HttpsError('invalid-argument','Informe um e-mail válido.');
  if(!subject||subject.length>140)throw new HttpsError('invalid-argument','Assunto inválido ou muito longo.');
  if(!ALLOWED_TEMPLATES.has(templateType))throw new HttpsError('invalid-argument','Template de e-mail não permitido.');
  if(user.role!=='admin_valora'&&!['invite','result'].includes(templateType))throw new HttpsError('permission-denied','Sua empresa só pode enviar convites e resultados.');
  const survey=await assertSurveyCompany(data.surveyId,user);
  if(data.responseId&&user.role!=='admin_valora'){
    const rs=await db.collection('responses').doc(String(data.responseId)).get();
    if(!rs.exists||rs.data().companyId!==user.companyId)throw new HttpsError('permission-denied','Resultado fora da sua empresa.');
  }
  await rateLimit(`emailSend:${user.uid}`,20,60*60*1000);
  const cfg=publicEmailConfig();if(!cfg.configured||!SMTP_PASSWORD.value())throw new HttpsError('failed-precondition','Envio de e-mail não configurado.');
  const transporter=nodemailer.createTransport({host:process.env.SMTP_HOST,port:Number(process.env.SMTP_PORT||587),secure:String(process.env.SMTP_SECURITY||'starttls')==='ssl',auth:{user:process.env.SMTP_USERNAME,pass:SMTP_PASSWORD.value()}});
  const payload=asObject(data.payload||{},'payload');
  const info=await transporter.sendMail({from:{name:process.env.SMTP_SENDER_NAME||'Valora Group',address:process.env.SMTP_SENDER_EMAIL||process.env.SMTP_USERNAME},to,subject,text:cleanText(payload.text||data.text||payload.message,4000),html:cleanText(data.html,20000)||buildEmailHtml(templateType,{...payload,subject})});
  await auditLog(db,{action:'sendEmail',actorId:user.uid,actorType:user.role,companyId:survey?.companyId||user.companyId,entity:'email',entityId:info.messageId,after:{to,templateType,surveyId:data.surveyId||'',responseId:data.responseId||''},ip:ip(req),userAgent:ua(req)});
  return {sent:true,messageId:info.messageId};
});
exports.lookupCep=onCall(async req=>{const cep=onlyDigits(req.data?.cep);if(cep.length!==8)throw new HttpsError('invalid-argument','Informe um CEP com 8 dígitos.');await rateLimit(`cep:${req.auth?.uid||ip(req)||'anon'}`,60,15*60*1000);let d;try{d=await callJson(`https://viacep.com.br/ws/${cep}/json/`,'CEP não encontrado.');if(d.erro)throw new Error('CEP não encontrado.');}catch(_){d=await callJson(`https://brasilapi.com.br/api/cep/v2/${cep}`,'CEP não encontrado.');}return {cep,logradouro:d.logradouro||d.street||'',bairro:d.bairro||d.neighborhood||'',localidade:d.localidade||d.city||'',uf:d.uf||d.state||'',address:[d.logradouro||d.street,d.bairro||d.neighborhood,d.localidade||d.city,d.uf||d.state].filter(Boolean).join(', ')};});
exports.lookupCnpj=onCall(async req=>{const user=await authedUser(req),cnpj=onlyDigits(req.data?.cnpj);if(cnpj.length!==14)throw new HttpsError('invalid-argument','Informe um CNPJ com 14 dígitos.');await rateLimit(`cnpj:${user.uid}`,30,15*60*1000);const d=await callJson(`https://brasilapi.com.br/api/cnpj/v1/${cnpj}`,'CNPJ não encontrado.');return {razao_social:d.razao_social||'',nome_fantasia:d.nome_fantasia||'',cnpj:d.cnpj||cnpj,endereco:[d.descricao_tipo_de_logradouro,d.logradouro,d.numero,d.bairro].filter(Boolean).join(', '),municipio:d.municipio||'',uf:d.uf||'',situacao_cadastral:d.descricao_situacao_cadastral||d.situacao_cadastral||'',cep:d.cep||''};});


async function auditScheduledReminder(name){
  await auditLog(db,{action:name,actorType:'system',entity:'notification',entityId:'scheduled',after:{status:'prepared_todo',note:'Estrutura segura preparada; envio automático deve validar plano, receivesEmail, e-mail válido, LGPD e deduplicação.'}});
}
exports.scheduledGenerateNotifications=onSchedule('every 24 hours',async()=>auditScheduledReminder('scheduledGenerateNotifications'));
exports.scheduledSendPendingReminders=onSchedule('every 24 hours',async()=>auditScheduledReminder('scheduledSendPendingReminders'));
exports.scheduledExpireInvitations=onSchedule('every 24 hours',async()=>auditScheduledReminder('scheduledExpireInvitations'));
exports.scheduledMarkOverdueActions=onSchedule('every 24 hours',async()=>auditScheduledReminder('scheduledMarkOverdueActions'));

const payments = require('./payments/payment-provider');

function assertValoraAdmin(user) {
  if (user.role !== 'admin_valora') throw new HttpsError('permission-denied', 'Apenas Admin Valora pode operar cobrança.');
}

exports.createPaymentLink = onCall(async req => {
  const user = await authedUser(req); assertValoraAdmin(user);
  const invoiceId = required(req.data, 'invoiceId');
  const snap = await db.collection('invoices').doc(invoiceId).get();
  if (!snap.exists) throw new HttpsError('not-found', 'Fatura não encontrada.');
  const invoice = { id: snap.id, ...snap.data() };
  const provider = invoice.paymentProvider || 'manual';
  const result = await payments.createPaymentLink(invoice, provider);
  await snap.ref.set({ paymentProvider: provider, paymentUrl: result.paymentUrl || '', updatedAt: TS(), updatedBy: user.uid }, { merge: true });
  await auditLog(db, { action: 'payment_link_created', actorId: user.uid, actorType: user.role, companyId: invoice.companyId, entity: 'invoice', entityId: invoiceId, after: { provider } });
  return result;
});

exports.createCheckoutSession = onCall(async req => {
  const user = await authedUser(req); assertValoraAdmin(user);
  throw new HttpsError('failed-precondition', 'Checkout real ainda não configurado. Configure provider e secrets no backend.');
});
exports.syncSubscriptionStatus = onCall(async req => { const user = await authedUser(req); assertValoraAdmin(user); return { synced: false, mode: 'manual' }; });
exports.cancelSubscription = onCall(async req => { const user = await authedUser(req); assertValoraAdmin(user); return { cancelled: false, mode: 'manual' }; });
exports.upgradeSubscription = onCall(async req => { const user = await authedUser(req); assertValoraAdmin(user); return { upgraded: false, mode: 'manual' }; });
exports.downgradeSubscription = onCall(async req => { const user = await authedUser(req); assertValoraAdmin(user); return { downgraded: false, mode: 'manual' }; });

exports.handlePaymentWebhook = onCall(async req => {
  const provider = cleanText(req.data?.provider || 'manual', 40);
  if (provider !== 'manual') throw new HttpsError('failed-precondition', 'Webhooks reais devem usar endpoint HTTP com validação criptográfica de assinatura.');
  const result = await payments.handleWebhook({ ...req.data, provider }, provider, { db });
  await auditLog(db, { action: 'payment_webhook_received', actorType: 'system', entity: 'webhook', entityId: provider, after: result });
  return result;
});
