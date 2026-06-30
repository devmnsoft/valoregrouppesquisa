#!/usr/bin/env node
const crypto=require('crypto');
const admin=require('firebase-admin');
const {getFirestore,validateFirebaseAdminCredentials,projectId}=require('./firebase-admin-client');
const args=process.argv.slice(2);
const OFFICIAL_FREE_SURVEY_ID='official_free_survey';
const OFFICIAL_FREE_FORM_ID='form_valora_insight_oficial';
const OFFICIAL_FREE_COMPANY_ID='valora-oficial';
const OFFICIAL_FREE_ORG_SLUG='valora-group';
const sha256=v=>crypto.createHash('sha256').update(String(v||''),'utf8').digest('hex');
const publicToken=()=>crypto.randomBytes(32).toString('base64url');
const nowIso=()=>new Date().toISOString();
const ts=()=>admin.firestore.FieldValue.serverTimestamp();
function tokenFromSurvey(s){const t=String(s?.publicToken||s?.token||s?.accessToken||'').trim();return t&&t!==String(s?.tokenHash||'')?t:'';}
const questions=[
{id:'q1',text:'A empresa possui processos internos bem definidos?',dimensionId:'gestao',dimensionName:'Gestão e Processos',type:'scale',required:true,weight:1,maxScore:5},
{id:'q2',text:'A empresa acompanha indicadores para tomar decisões?',dimensionId:'indicadores',dimensionName:'Indicadores e Decisão',type:'scale',required:true,weight:1,maxScore:5},
{id:'q3',text:'A empresa utiliza sistemas para reduzir tarefas manuais?',dimensionId:'tecnologia',dimensionName:'Tecnologia e Automação',type:'scale',required:true,weight:1,maxScore:5},
{id:'q4',text:'As informações importantes estão organizadas e fáceis de acessar?',dimensionId:'informacao',dimensionName:'Organização da Informação',type:'scale',required:true,weight:1,maxScore:5},
{id:'q5',text:'A comunicação interna é clara e bem estruturada?',dimensionId:'comunicacao',dimensionName:'Comunicação',type:'scale',required:true,weight:1,maxScore:5},
{id:'q6',text:'A empresa possui controles para acompanhar prazos, demandas e responsáveis?',dimensionId:'controle',dimensionName:'Controle Operacional',type:'scale',required:true,weight:1,maxScore:5},
{id:'q7',text:'A empresa está preparada para crescer sem perder organização?',dimensionId:'crescimento',dimensionName:'Crescimento e Escala',type:'scale',required:true,weight:1,maxScore:5}
];
const resultBands=[
{from:0,to:1.99,title:'Maturidade inicial',recommendation:'Sua empresa possui oportunidades importantes de organização, controle e automação.'},
{from:2,to:3.49,title:'Maturidade intermediária',recommendation:'Sua empresa já possui boas práticas, mas ainda pode evoluir em processos, tecnologia e indicadores.'},
{from:3.5,to:5,title:'Maturidade avançada',recommendation:'Sua empresa demonstra boa estrutura e pode avançar com otimização, integração e escala.'}
];
function printOfficialUrls(token){console.log(`URL oficial domínio: https://valoragroup.mnsoft.com.br/?survey=${OFFICIAL_FREE_SURVEY_ID}&token=${token}&org=${OFFICIAL_FREE_ORG_SLUG}`);console.log(`URL oficial Firebase: https://gestordepesquisa.web.app/?survey=${OFFICIAL_FREE_SURVEY_ID}&token=${token}&org=${OFFICIAL_FREE_ORG_SLUG}`);}
(async()=>{const cred=validateFirebaseAdminCredentials({args});if(!cred.ok){const token=publicToken();console.warn(cred.message);console.warn('Seed não aplicado: credenciais Firebase Admin indisponíveis neste ambiente.');printOfficialUrls(token);return;}const db=getFirestore({args});const now=ts();const existingSnap=await db.collection('surveys').doc(OFFICIAL_FREE_SURVEY_ID).get();const existing=existingSnap.exists?existingSnap.data():{};const token=tokenFromSurvey(existing)||publicToken();const org={id:OFFICIAL_FREE_COMPANY_ID,companyId:OFFICIAL_FREE_COMPANY_ID,organizationId:OFFICIAL_FREE_COMPANY_ID,name:'Valora Group',publicName:'Valora Group',slug:OFFICIAL_FREE_ORG_SLUG,status:'active',planId:'free',isDeleted:false,createdAt:existing.createdAt||now,updatedAt:now};const form={id:OFFICIAL_FREE_FORM_ID,companyId:OFFICIAL_FREE_COMPANY_ID,name:'Valora Insight™',title:'Diagnóstico Gratuito Valora Insight™',description:'Diagnóstico gratuito para avaliar maturidade, organização, tecnologia e oportunidades de melhoria.',status:'active',timeMin:5,scoringMethod:'weightedAverage',lgpdText:'Ao responder este diagnóstico, você autoriza o tratamento dos dados informados para geração do resultado, histórico e contato relacionado às soluções da Valora Group.',questions,resultBands,createdAt:existing.createdAt||now,updatedAt:now};const survey={id:OFFICIAL_FREE_SURVEY_ID,companyId:OFFICIAL_FREE_COMPANY_ID,organizationId:OFFICIAL_FREE_COMPANY_ID,formId:OFFICIAL_FREE_FORM_ID,title:'Diagnóstico Gratuito Valora Insight™',description:'Responda gratuitamente e receba uma análise inicial sobre a maturidade da sua empresa.',status:'published',visibility:'public',isFree:true,planId:'free',requireIdentification:true,lgpdRequired:true,allowRepeat:true,anonymous:false,showResult:true,revoked:false,revokedAt:null,publicToken:token,token,tokenHash:sha256(token),responseCount:Number(existing.responseCount||0),startsAt:existing.startsAt||nowIso(),expiresAt:'2099-12-31T23:59:59.000Z',createdAt:existing.createdAt||now,updatedAt:now,featuredOnHome:true,visibleOnHome:true,isFeatured:true};const batch=db.batch();batch.set(db.collection('organizations').doc(OFFICIAL_FREE_COMPANY_ID),org,{merge:true});batch.set(db.collection('companies').doc(OFFICIAL_FREE_COMPANY_ID),org,{merge:true});batch.set(db.collection('forms').doc(OFFICIAL_FREE_FORM_ID),form,{merge:true});batch.set(db.collection('surveys').doc(OFFICIAL_FREE_SURVEY_ID),survey,{merge:true});await batch.commit();console.log(`Pesquisa oficial garantida no projeto ${projectId({args})||cred.projectId||'(default)'}.`);printOfficialUrls(token);})().catch(e=>{console.error(e.stack||e.message);process.exit(1);});
