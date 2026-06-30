(function(){
'use strict';

const ALLOWED_ROLES=Object.keys(window.ValoraRoles?.ROLE_DEFINITIONS||{admin_valora:1,consultor_valora:1,empresa_admin:1,gestor_pesquisa:1,analista_resultados:1,gestor_area:1,participante:1,convidado_externo:1});
const GLOBAL_ROLES=['admin_valora','consultor_valora'];
const PROFILE_MISSING_MESSAGE='Cadastro incompleto. Solicite liberação ao administrador.';
const FRIENDLY_LOAD_ERROR='Não foi possível carregar as informações. Tente novamente.';
const REQUIRED_FRONTEND_COLLECTIONS=['settings','modules','plans','organizations','companies','users','forms','surveys','responses','invitations','invoices','actionPlans','notifications','knowledgeBase','supportCategories','supportSlaPolicies','supportTickets','supportMessages','integrations','webhooks','apiKeys','communications','units','serviceDeliverables'];
const session={authUser:null,profile:null,claims:{},ready:false,readyPromise:null,unsubscribe:null,store:null,normalizeState:null,loaded:false,loading:false,lastError:null,cache:{},authDebug:null};

function services(){return window.ValoraFirebaseServices||{};}
function ensureFirebase(){const s=services();if(!s.initialized||!s.auth||!s.db)throw Object.assign(new Error('Serviço de autenticação indisponível. Tente novamente mais tarde.'),{code:'network'});return s;}
function firestore(){return ensureFirebase().db;}
function fv(){return window.firebase.firestore.FieldValue;}
function ts(){return fv().serverTimestamp();}
function callFunction(name,payload){const s=ensureFirebase();if(!s.functions)throw Object.assign(new Error('Cloud Functions não configurado.'),{code:'functions-unavailable'});return s.functions.httpsCallable(name)(payload).then(r=>r.data);}
function clone(v){return JSON.parse(JSON.stringify(v||null));}
function publicProfile(doc,claims={}){if(!doc)return null;const role=ALLOWED_ROLES.includes(claims.role)?claims.role:doc.role;return {id:doc.uid||doc.id,uid:doc.uid||doc.id,name:doc.name||doc.email,email:doc.email,phone:doc.phone||'',position:doc.position||'',department:doc.department||'',role,companyId:doc.companyId||claims.companyId||'',status:doc.status||'inactive',preferences:doc.preferences||{},claims};}
function cleanFirebaseLocalState(){try{Object.keys(localStorage).filter(k=>k.startsWith('firebase:')||k.includes('ValoraFirebase')).forEach(k=>localStorage.removeItem(k));}catch(_){} }
function docData(d){return d.exists?{id:d.id,...d.data()}:null;}
function asArray(snap){return snap.docs.map(docData).filter(Boolean);}
function isGlobalRole(profile=session.profile){return GLOBAL_ROLES.includes(profile?.role);}
function actorId(){return session.profile?.uid||session.profile?.id||session.authUser?.uid||'system';}
function normalizeCompany(org){if(!org)return org;const fallback='Empresa sem nome';return {...org,id:org.id||org.uid,name:org.name||org.publicName||org.legalName||fallback,publicName:org.publicName||org.name||org.legalName||fallback,legalName:org.legalName||org.name||org.publicName||fallback,planId:org.planId||org.subscription?.planId||'essential',status:org.status||'active'};}
function collectionNameForStateKey(key){return ({companies:'organizations'})[key]||key;}
function collectionStateKey(collection){return ({organizations:'companies'})[collection]||collection;}
function canUseCompanyScope(){return !!session.profile?.companyId&&!isGlobalRole();}
function buildScopeFilter(companyId){return companyId?[['companyId','==',companyId]]:[];}
function firestoreDebug(){window.ValoraFirestoreDebug=window.ValoraFirestoreDebug||{errors:[],collections:{},auth:{}};return window.ValoraFirestoreDebug;}
function recordFirestoreError(collectionName,err){const dbg=firestoreDebug();const item={collection:collectionName,code:err?.code||'',message:err?.message||String(err),createdAt:new Date().toISOString()};dbg.errors.push(item);dbg.lastError=item;if(dbg.errors.length>80)dbg.errors=dbg.errors.slice(-80);console.error(`[Valora Pulse] Erro ao carregar ${collectionName}`,err);}
function recordCollectionDebug(collectionName,count,meta={}){const dbg=firestoreDebug();dbg.collections[collectionName]={count:Number(count||0),loadedAt:new Date().toISOString(),...meta};}
function mapCollectionError(err){
  const code=err?.code||'';
  if(code==='permission-denied')return Object.assign(new Error('Seu perfil não possui permissão para acessar estes dados.'),{code});
  if(code==='unauthenticated'||code==='auth/user-token-expired')return Object.assign(new Error('Sua sessão expirou. Entre novamente.'),{code});
  if(code==='unavailable'||code.includes('network'))return Object.assign(new Error('Não foi possível carregar as informações. Tente novamente.'),{code});
  return Object.assign(new Error(FRIENDLY_LOAD_ERROR),{code});
}
async function refreshAuthDebug(user=session.authUser){if(!user)return {};await user.getIdToken(true);const tokenResult=await user.getIdTokenResult(true);const claims=tokenResult.claims||{};session.authDebug={uid:user.uid,email:user.email,claims,refreshedAt:new Date().toISOString(),tokenRefreshed:true,hasRoleClaim:!!claims.role};firestoreDebug().auth=session.authDebug;if(session.store)session.store.authDebug=session.authDebug;if(!claims.role)console.warn('[Valora Pulse] Usuário autenticado, mas sem custom claim role. Faça logout/login ou reaplique claims.');return claims;}
async function getClaims(user=session.authUser){if(!user)return {};return refreshAuthDebug(user);}
async function getCurrentAuthUser(){await waitUntilReady();return session.authUser;}
async function getCurrentUserProfile(){await waitUntilReady();return session.profile;}
async function loadProfile(user){
  if(!user)return null;
  const s=ensureFirebase();const claims=await getClaims(user);const ref=s.db.collection('users').doc(user.uid);const snap=await ref.get();
  if(!snap.exists)throw Object.assign(new Error(PROFILE_MISSING_MESSAGE),{code:'profile-missing'});
  const doc={uid:user.uid,...snap.data()};const profile=publicProfile(doc,claims);
  if(profile.status!=='active')throw Object.assign(new Error('Usuário inativo.'),{code:'inactive-user'});
  await ref.set({lastLoginAt:ts(),updatedAt:doc.updatedAt||ts()},{merge:true});
  session.claims=claims;session.profile=profile;return profile;
}
function waitUntilReady(){
  if(session.ready)return Promise.resolve(session.profile);
  if(session.readyPromise)return session.readyPromise;
  const s=ensureFirebase();
  session.readyPromise=new Promise(resolve=>{
    session.unsubscribe=s.auth.onAuthStateChanged(async user=>{
      session.authUser=user;session.profile=null;session.claims={};session.loaded=false;
      try{if(user)await loadProfile(user);}catch(err){console.warn('[Valora Pulse] Sessão bloqueada.',err);try{await s.auth.signOut();}catch(_){} }
      session.ready=true;resolve(session.profile);
      if(session.profile&&session.store) hydrateStore().finally(()=>dispatchAuthChanged()); else dispatchAuthChanged();
    });
  });
  return session.readyPromise;
}
function dispatchAuthChanged(){window.dispatchEvent(new CustomEvent('valora:auth-changed',{detail:{user:session.profile,loaded:session.loaded,error:session.lastError}}));}
function emptyStore(seedStore,normalizeState){const state=seedStore();Object.assign(state,{session:null,users:[],companies:[],forms:[],surveys:[],responses:[],invitations:[],invoices:[],actionPlans:[],supportTickets:[],supportMessages:[],supportSlaPolicies:[],supportCategories:[],knowledgeBase:[],integrations:[],apiKeys:[],webhooks:[],integrationLogs:[],logs:[]});normalizeState(state);return state;}
async function queryCollection(name,filters=[],orderBy=null,limit=null){
  try{let q=firestore().collection(name);filters.forEach(([field,op,value])=>{if(value!==undefined&&value!==null&&value!=='')q=q.where(field,op,value);});if(orderBy)q=q.orderBy(orderBy);if(limit)q=q.limit(limit);const rows=asArray(await q.get());recordCollectionDebug(name,rows.length,{filters:filters.map(f=>f.join(' '))});return rows;}
  catch(err){recordFirestoreError(name,err);throw mapCollectionError(err);}
}
async function getDoc(name,id){try{const row=docData(await firestore().collection(name).doc(id).get());recordCollectionDebug(name,row?1:0,{docId:id,docExists:!!row});return row;}catch(err){recordFirestoreError(name,err);throw mapCollectionError(err);}}
function metadata(data={},creating=false){const profile=session.profile||{};const out={...data,updatedAt:ts(),updatedBy:actorId()};if(creating){out.createdAt=data.createdAt||ts();out.createdBy=data.createdBy||actorId();}if(!out.companyId&&profile.companyId&&!['plans','modules','settings'].includes(data.__collection||''))out.companyId=profile.companyId;delete out.__collection;return out;}
async function createDoc(name,data){const ref=data.id?firestore().collection(name).doc(data.id):firestore().collection(name).doc();const payload=metadata({...data,id:ref.id,__collection:name},true);await ref.set(payload,{merge:false});return {id:ref.id,...data};}
async function updateDoc(name,id,data){const safe={...data};delete safe.id;delete safe.createdAt;delete safe.createdBy;await firestore().collection(name).doc(id).set(metadata({...safe,__collection:name}),{merge:true});return {id,...data};}
async function deleteDoc(name,id){await firestore().collection(name).doc(id).delete();return true;}
async function scopedRows(collection,companyId){const p=session.profile;if(!p)return [];if(isGlobalRole(p))return queryCollection(collection,buildScopeFilter(companyId));return queryCollection(collection,buildScopeFilter(companyId||p.companyId));}
async function loadOrganizations(){const p=session.profile;if(!p)return [];if(isGlobalRole(p))return queryCollection('organizations');const org=await getDoc('organizations',p.companyId);if(org?.status==='inactive')throw Object.assign(new Error('Sua empresa está inativa. Contate o administrador.'),{code:'inactive-company'});return org?[org]:[];}
async function loadUsers(companyId){const p=session.profile;if(!p)return [];if(isGlobalRole(p))return queryCollection('users',buildScopeFilter(companyId));return queryCollection('users',buildScopeFilter(companyId||p.companyId));}
async function loadForms(companyId){const p=session.profile;if(!p)return [];if(isGlobalRole(p)&&!companyId)return queryCollection('forms');const cid=companyId||p.companyId;const [owned,globalA,globalB]=await Promise.all([queryCollection('forms',buildScopeFilter(cid)),queryCollection('forms',[['companyId','==','global']]),queryCollection('forms',[['isGlobal','==',true]])]);return [...new Map([...owned,...globalA,...globalB].map(x=>[x.id,x])).values()];}
async function loadSurveys(companyId){const p=session.profile;if(!p)return [];if(isGlobalRole(p)&&!companyId)return queryCollection('surveys');return queryCollection('surveys',buildScopeFilter(companyId||p.companyId));}
async function loadResponses(companyId){const p=session.profile;if(!p)return [];if(p.role==='participante')return queryCollection('responses',[['participantUid','==',p.uid||p.id]]);if(isGlobalRole(p)&&!companyId)return queryCollection('responses');let filters=buildScopeFilter(companyId||p.companyId);if(p.role==='gestor_area'&&p.department)filters.push(['department','==',p.department]);return queryCollection('responses',filters);}
async function loadSettings(){const p=session.profile;if(!p)return {};if(p.role==='admin_valora'){const rows=await queryCollection('settings');return Object.fromEntries(rows.map(x=>[x.id,x]));}return await getDoc('settings','public')||{};}
async function loadCompaniesRaw(companyId){const p=session.profile;if(!p)return [];if(isGlobalRole(p)&&!companyId)return queryCollection('companies');return queryCollection('companies',buildScopeFilter(companyId||p.companyId));}
async function loadStoreData(){
  const p=session.profile;if(!p)return null;
  const canFinance=['admin_valora','empresa_admin'].includes(p.role);
  const [organizations,companiesRaw,users,plans,modules,forms,surveys,responses,invitations,invoices,actionPlans,supportTickets,supportMessages,supportSlaPolicies,supportCategories,knowledgeBase,notifications,settings,logs,integrations,apiKeys,webhooks,integrationLogs,communications,units,serviceDeliverables]=await Promise.all([
    loadOrganizations(),loadCompaniesRaw(),loadUsers(),queryCollection('plans'),queryCollection('modules'),loadForms(),loadSurveys(),loadResponses(),scopedRows('invitations'),canFinance?scopedRows('invoices'):Promise.resolve([]),scopedRows('actionPlans'),scopedRows('supportTickets'),scopedRows('supportMessages'),queryCollection('supportSlaPolicies'),queryCollection('supportCategories'),queryCollection('knowledgeBase'),scopedRows('notifications'),loadSettings(),p.role==='admin_valora'?queryCollection('logs',[],null,300):Promise.resolve([]),scopedRows('integrations'),scopedRows('apiKeys'),scopedRows('webhooks'),scopedRows('integrationLogs'),scopedRows('communications'),scopedRows('units'),scopedRows('serviceDeliverables')
  ]);
  const companiesFromOrganizations=organizations.map(normalizeCompany);
  const companies=companiesFromOrganizations.length?companiesFromOrganizations:companiesRaw.map(normalizeCompany);
  const compatibleOrganizations=organizations.length?organizations:companiesRaw.map(normalizeCompany);
  return {session:{userId:p.id||p.uid,createdAt:new Date().toISOString()},authDebug:session.authDebug,organizations:compatibleOrganizations,companies,users,plans,modules,forms,surveys,responses,invitations,invoices,actionPlans,supportTickets,supportMessages,supportSlaPolicies,supportCategories,knowledgeBase,notifications,settings,logs,integrations,apiKeys,webhooks,integrationLogs,communications,units,serviceDeliverables,firestoreLastError:null,firestoreDebug:firestoreDebug()};
}
async function hydrateStore(){
  if(session.loading)return;session.loading=true;session.lastError=null;
  try{const data=await loadStoreData();if(!data)return;Object.assign(session.store,data);session.normalizeState?.(session.store);REQUIRED_FRONTEND_COLLECTIONS.forEach(k=>recordCollectionDebug(k,k==='settings'?Object.keys(session.store.settings||{}).length:(session.store[k]||[]).length,{stateKey:k}));session.cache=clone({companies:session.store.companies,users:session.store.users,plans:session.store.plans,modules:session.store.modules,forms:session.store.forms,surveys:session.store.surveys,responses:session.store.responses,invitations:session.store.invitations,invoices:session.store.invoices,actionPlans:session.store.actionPlans,supportTickets:session.store.supportTickets,supportMessages:session.store.supportMessages,supportSlaPolicies:session.store.supportSlaPolicies,supportCategories:session.store.supportCategories,knowledgeBase:session.store.knowledgeBase,notifications:session.store.notifications,settings:session.store.settings,communications:session.store.communications,units:session.store.units,serviceDeliverables:session.store.serviceDeliverables});session.loaded=true;}
  catch(err){session.lastError=err;if(session.store){session.store.firestoreLastError={code:err?.code||'',message:err?.message||String(err),at:new Date().toISOString()};session.store.firestoreDebug=firestoreDebug();}console.warn('[Valora Pulse] Falha ao carregar Firestore.',err);}
  finally{session.loading=false;}
}
function changed(a,b){return JSON.stringify(a||null)!==JSON.stringify(b||null);}
async function syncCollectionFromState(key){const name=collectionNameForStateKey(key);const before=new Map((session.cache[key]||[]).map(x=>[x.id,x]));const now=new Map((session.store[key]||[]).map(x=>[x.id,x]));const writes=[];now.forEach((row,id)=>{if(key==='users'&&(!row.uid||row.uid!==id)&&!before.has(id)){console.warn('[Valora Pulse] TODO seguro: criação de usuário Firebase Auth deve ocorrer via Cloud Function/Admin SDK antes de gravar users/{uid}.',row.email);return;}if(!before.has(id))writes.push(createDoc(name,row));else if(changed(row,before.get(id)))writes.push(updateDoc(name,id,row));});before.forEach((_row,id)=>{if(!now.has(id))writes.push(deleteDoc(name,id));});await Promise.all(writes);session.cache[key]=clone(session.store[key]||[]);}
async function syncSettings(){if(!changed(session.store.settings,session.cache.settings))return;const value=session.store.settings||{};if(value.public||Object.keys(value).some(k=>typeof value[k]==='object'))await Promise.all(Object.entries(value).map(([id,data])=>updateDoc('settings',id,data)));else await updateDoc('settings','public',value);session.cache.settings=clone(value);}


function makeSlugBase(value){return String(value||'empresa').normalize('NFD').replace(/[\u0300-\u036f]/g,'').toLowerCase().replace(/[^a-z0-9]+/g,'-').replace(/^-|-$/g,'').slice(0,60)||'empresa';}
async function uniqueOrganizationSlug(base){let slug=makeSlugBase(base),candidate=slug,i=2;while(true){const rows=await queryCollection('organizations',[["slug","==",candidate]],null,1).catch(()=>[]);if(!rows.length)return candidate;candidate=`${slug}-${i++}`;}}
async function registerCompany(data={}){
  const s=ensureFirebase();
  const name=String(data.name||data.legalName||data.companyName||'').trim();
  if(!name)throw Object.assign(new Error('Informe o nome da empresa.'),{code:'company_name_required'});
  const email=String(data.email||data.responsibleEmail||'').trim().toLowerCase();
  const responsibleEmail=String(data.responsibleEmail||data.email||'').trim().toLowerCase();
  const document=String(data.document||data.cnpj||'').trim();
  const now=new Date().toISOString();
  const ref=s.db.collection('organizations').doc();
  const slug=await uniqueOrganizationSlug(data.slug||name);
  const status=data.status||'active', planId=data.planId||'free';
  const company={id:ref.id,companyId:ref.id,organizationId:ref.id,type:data.type||'juridica',name,publicName:data.publicName||name,legalName:data.legalName||name,document,email,phone:data.phone||'',responsibleName:data.responsibleName||data.name||'',responsibleEmail,slug,planId,status,subscription:data.subscription||{planId,status,billingStatus:'free',startedAt:now},createdAt:now,updatedAt:now,source:data.source||'public_register_company'};
  const batch=s.db.batch();
  batch.set(ref,company,{merge:true});
  batch.set(s.db.collection('companies').doc(ref.id),company,{merge:true});
  await batch.commit();
  return {ok:true,id:ref.id,companyId:ref.id,organizationId:ref.id,slug,status};
}

async function registerCompanyAccount(data){
  const s=ensureFirebase();
  const email=String(data.email||'').trim().toLowerCase();
  if(!email||!data.password)throw Object.assign(new Error('Informe e-mail e senha para criar o ambiente.'),{code:'invalid-signup'});
  const cred=await s.auth.createUserWithEmailAndPassword(email,data.password);
  const uid=cred.user.uid;
  const now=new Date().toISOString();
  const companyRef=s.db.collection('organizations').doc();
  const legacyCompanyRef=s.db.collection('companies').doc(companyRef.id);
  const settingsRef=s.db.collection('organizationSettings').doc(companyRef.id);
  const company={id:companyRef.id,type:data.type||'juridica',name:data.companyName||data.name||email,publicName:data.companyName||data.name||email,slug:data.slug||String(data.companyName||email).normalize('NFD').replace(/[\u0300-\u036f]/g,'').toLowerCase().replace(/[^a-z0-9]+/g,'-').replace(/^-|-$/g,''),document:data.document||'',email,phone:data.phone||'',cep:data.cep||'',address:data.address||'',planId:data.planId||'free',status:'active',subscription:{planId:data.planId||'free',status:'active',billingStatus:'free',startedAt:now},createdAt:now,updatedAt:now};
  const user={id:uid,uid,name:data.name||data.companyName||email,email,role:'empresa_admin',companyId:company.id,phone:data.phone||'',status:'active',receivesEmail:true,portalAccess:true,createdAt:now,updatedAt:now};
  const batch=s.db.batch();
  batch.set(companyRef,company);
  batch.set(legacyCompanyRef,{...company,organizationId:company.id});
  batch.set(settingsRef,{organizationId:company.id,companyId:company.id,planId:company.planId,status:company.status,createdAt:now,updatedAt:now,notifications:{email:true},lgpd:{enabled:true}});
  batch.set(s.db.collection('users').doc(uid),user);
  await batch.commit();
  const profile=await loadProfile(cred.user);
  await hydrateStore();
  return profile;
}
function mapAuthError(err){const code=err?.code||'';/* auth/invalid-login-credentials auth/user-not-found auth/wrong-password auth/invalid-email auth/user-disabled auth/too-many-requests auth/network-request-failed auth/api-key-not-valid auth/operation-not-allowed */if(code.includes('invalid-credential')||code.includes('invalid-login-credentials')||code.includes('wrong-password')||code.includes('user-not-found'))return 'E-mail ou senha inválidos.';if(code.includes('invalid-email'))return 'E-mail inválido.';if(code.includes('user-disabled'))return 'Usuário desativado.';if(code.includes('operation-not-allowed')||code.includes('api-key-not-valid')||code.includes('configuration-not-found'))return 'Serviço de autenticação indisponível.';if(code.includes('too-many-requests'))return 'Muitas tentativas. Aguarde alguns minutos.';if(code.includes('network-request-failed')||code==='unavailable')return 'Serviço de autenticação indisponível.';if(code==='inactive-user')return 'Usuário inativo. Solicite liberação ao administrador.';if(code==='profile-missing')return PROFILE_MISSING_MESSAGE;if(code==='inactive-company')return 'Sua empresa está inativa. Contate o administrador.';return 'Não foi possível entrar. Verifique seus dados ou solicite redefinição de senha.';}


function isOfficialFreeSurveyDoc(survey){
  if(!survey)return false;
  const id=String(survey.id||survey.surveyId||'').toLowerCase();
  if(id==='survey_demo'||id.includes('demo')||id.includes('exemplo')||id.includes('example'))return false;
  const title=String(survey.title||survey.name||'').toLowerCase();
  const status=String(survey.status||'').toLowerCase();
  const visibility=String(survey.visibility||survey.publicVisibility||'public').toLowerCase();
  const freeByTitle=title.includes('valora insight')||title.includes('diagnóstico gratuito')||title.includes('diagnostico gratuito')||title.includes('pesquisa gratuita');
  return publicTokenFromSurvey(survey)&&['active','published','open'].includes(status)&&survey.revoked!==true&&!survey.revokedAt&&!['private','restricted','internal'].includes(visibility)&&(id==='official_free_survey'||survey.organizationId==='valora-oficial'||survey.companyId==='valora-oficial'||survey.official===true||survey.isOfficial===true||survey.isFree===true||survey.planId==='free'||freeByTitle);
}
function publicTokenFromSurvey(survey){const t=survey?.publicToken||survey?.token||survey?.accessToken||'';return t&&String(t)!==String(survey?.tokenHash||'')?t:'';}
async function loadOfficialFreeSurveyPublic(){
  const attempted=[];let rows=[];
  async function tryQuery(label,filters){attempted.push(label);try{rows=rows.concat(await queryCollection('surveys',filters,null,25));}catch(err){recordFirestoreError(`surveys.${label}`,err);}}
  attempted.push('official_free_survey');
  try{const official=await getDoc('surveys','official_free_survey');if(official)rows.push(official);}catch(err){recordFirestoreError('surveys.official_free_survey',err);}
  await tryQuery('isFree', [['isFree','==',true]]);
  await tryQuery('planIdFree', [['planId','==','free']]);
  if(session.store?.surveys?.length)rows=rows.concat(session.store.surveys);
  const unique=[...new Map(rows.filter(Boolean).map(x=>[x.id,x])).values()];
  const candidates=unique.filter(isOfficialFreeSurveyDoc);
  let selected=candidates.find(s=>String(s.id)==='official_free_survey')||candidates.find(publicTokenFromSurvey)||candidates[0]||null;
  if(selected&&!publicTokenFromSurvey(selected)&&selected.id){
    try{const repaired=await callFunction('repairFreeSurveyPublicLink',{surveyId:selected.id});selected={...selected,...(repaired?.survey||repaired?.after||repaired||{})};}catch(err){recordFirestoreError('surveys.repairFreeSurveyPublicLink',err);}
  }
  window.ValoraRuntimeDiagnostics=window.ValoraRuntimeDiagnostics||{};
  window.ValoraRuntimeDiagnostics.lastOfficialFreeSurvey={attemptedSources:attempted.concat(['session.store','repairFreeSurveyPublicLink']),reason:selected?'candidate_found':'no_valid_official_free_survey',foundCandidatesCount:candidates.length,hasPublicToken:!!publicTokenFromSurvey(selected),status:selected?.status||'',visibility:selected?.visibility||selected?.publicVisibility||'',isFree:selected?.isFree===true||selected?.planId==='free'};
  return selected&&publicTokenFromSurvey(selected)?selected:null;
}

function isFeaturedHomeSurveyDoc(survey){
  if(!isOfficialFreeSurveyDoc(survey))return false;
  return survey.featuredOnHome===true||survey.isFeatured===true;
}
async function companyForSurvey(survey){
  const id=survey?.companyId||survey?.organizationId||'';
  if(!id)return null;
  return await getDoc('organizations',id).catch(()=>null)||await getDoc('companies',id).catch(()=>null);
}
function featuredHomeSurveyUrl(survey,company={}){
  const token=publicTokenFromSurvey(survey);
  if(!survey?.id||!token)return '';
  const base=window.ValoraConfig?.APP_PUBLIC_URL||location.href.split('?')[0].split('#')[0];
  const url=new URL(base);
  url.searchParams.set('survey',survey.id);
  url.searchParams.set('token',token);
  url.searchParams.set('org',company?.slug||company?.publicSlug||(survey.id==='official_free_survey'?'valora-group':''));
  const text=url.toString();
  return /survey_demo|empresa-exemplo|tokenHash=|demo-token/i.test(text)?'':text;
}
async function resolveFeaturedHomeSurveyPublic(){
  const attempted=[];let rows=[];
  async function tryQuery(label,filters){attempted.push(label);try{rows=rows.concat(await queryCollection('surveys',filters,null,25));}catch(err){recordFirestoreError(`surveys.${label}`,err);}}
  await tryQuery('featuredOnHome',[['featuredOnHome','==',true]]);
  await tryQuery('isFeatured',[['isFeatured','==',true]]);
  if(session.store?.surveys?.length)rows=rows.concat(session.store.surveys);
  let unique=[...new Map(rows.filter(Boolean).map(x=>[x.id,x])).values()];
  let selected=unique.filter(isFeaturedHomeSurveyDoc).sort((a,b)=>Date.parse(b.updatedAt||b.createdAt||0)-Date.parse(a.updatedAt||a.createdAt||0))[0]||null;
  if(!selected)selected=await loadOfficialFreeSurveyPublic();
  const company=selected?await companyForSurvey(selected):null;
  const url=selected?featuredHomeSurveyUrl(selected,company):'';
  window.ValoraRuntimeDiagnostics=window.ValoraRuntimeDiagnostics||{};
  window.ValoraRuntimeDiagnostics.lastFeaturedHomeSurvey={attemptedSources:attempted.concat(['session.store','official_free_survey']),reason:url?'candidate_found':'no_valid_featured_home_survey',surveyId:selected?.id||'',hasPublicToken:!!publicTokenFromSurvey(selected),org:company?.slug||''};
  return url?{survey:selected,company,url,source:selected?.featuredOnHome||selected?.isFeatured?'firestore.featured':'official_free_survey'}:null;
}
async function getFeaturedHomeSurveyUrlPublic(){const r=await resolveFeaturedHomeSurveyPublic();return r?.url||'';}

async function validatePublicSurveyPublic({surveyId,token,org}={}){
  const survey=await getDoc('surveys',surveyId);
  if(!survey)throw Object.assign(new Error('Pesquisa não encontrada.'),{code:'survey_not_found'});
  const expected=publicTokenFromSurvey(survey);
  if(!expected||String(token)!==String(expected))throw Object.assign(new Error('Token público inválido.'),{code:'invalid_public_token'});
  if(!isOfficialFreeSurveyDoc(survey)&&!['active','published','open'].includes(String(survey.status||'').toLowerCase()))throw Object.assign(new Error('Pesquisa encerrada ou expirada.'),{code:'survey_inactive'});
  const form=await getDoc('forms',survey.formId);
  if(!form)throw Object.assign(new Error('Formulário não encontrado.'),{code:'form_not_found'});
  const company=survey.companyId||survey.organizationId?await getDoc('organizations',survey.companyId||survey.organizationId).catch(()=>null):null;
  if(org&&company?.slug&&String(org).toLowerCase()!==String(company.slug).toLowerCase())throw Object.assign(new Error('Organização pública não confere.'),{code:'org_mismatch'});
  return {ok:true,survey,form,company,lgpd:{text:''}};
}

window.ValoraFirebaseAuth={getCurrentAuthUser,getCurrentUserProfile,waitUntilReady};
window.ValoraFirebaseRepository={
  mode:'firebase',
  loadStore({seedStore,normalizeState}){cleanFirebaseLocalState();session.store=emptyStore(seedStore,normalizeState);session.normalizeState=normalizeState;waitUntilReady().catch(()=>{});return session.store;},
  saveStore(){return this.saveChanges({state:session.store});},
  async login({email,password}){const s=ensureFirebase();try{const cred=await s.auth.signInWithEmailAndPassword(email,password);await cred.user.getIdToken(true);const profile=await loadProfile(cred.user);await hydrateStore();return profile;}catch(err){throw Object.assign(new Error(mapAuthError(err)),{code:err?.code});}},
  registerCompanyAccount,
  registerCompany,
  async logout(){const s=ensureFirebase();session.profile=null;session.claims={};session.loaded=false;cleanFirebaseLocalState();await s.auth.signOut();},
  currentUser(){return session.profile;},
  async loadStoreFromFirestore(){await hydrateStore();return session.store;},
  loadOfficialFreeSurvey:loadOfficialFreeSurveyPublic,
  resolveFeaturedHomeSurvey:resolveFeaturedHomeSurveyPublic,
  getFeaturedHomeSurveyUrl:getFeaturedHomeSurveyUrlPublic,
  validatePublicSurvey:validatePublicSurveyPublic,
  loadCompanies({state}={}){return state?.companies||[];},loadOrganizations:loadOrganizations,loadUsers({state}={}){return state?.users||[];},loadPlans({state}={}){return state?.plans||[];},loadModules({state}={}){return state?.modules||[];},loadForms({state}={}){return state?.forms||[];},loadSurveys({state}={}){return state?.surveys||[];},loadResponses({state}={}){return state?.responses||[];},loadInvitations({state}={}){return state?.invitations||[];},loadActionPlans({state}={}){return state?.actionPlans||[];},loadSupportTickets({state}={}){return state?.supportTickets||[];},loadKnowledgeBase({state}={}){return state?.knowledgeBase||[];},loadNotifications({state}={}){return state?.notifications||[];},loadInvoices({state}={}){return state?.invoices||[];},loadIntegrations({state}={}){return state?.integrations||[];},loadApiKeys({state}={}){return (state?.apiKeys||[]).map(x=>({...x,keyHash:x.keyHash?'[protected]':''}));},loadWebhooks({state}={}){return (state?.webhooks||[]).map(x=>({...x,secretHash:x.secretHash?'[protected]':''}));},loadSettings({state}={}){return state?.settings||{};},
  listOrganizations:loadOrganizations,getOrganization(id){return getDoc('organizations',id);},createOrganization(data){return createDoc('organizations',data);},createClient(data){return callFunction('createClient',{payload:data}).catch(()=>createDoc('organizations',data));},updateClient(id,data){return callFunction('updateClient',{id,payload:data}).catch(()=>updateDoc('organizations',id,data));},updateOrganization(id,data){return updateDoc('organizations',id,data);},updateOrganizationBrand(id,brand){return updateDoc('organizations',id,{brand});},updateOrganizationSubscription(id,subscription){return updateDoc('organizations',id,{subscription,planId:subscription?.planId});},updateOrganizationSettings(id,settings){return updateDoc('organizations',id,{settings});},updateOrganizationLimits(id,limitsOverride){return updateDoc('organizations',id,{limitsOverride});},async getOrganizationBySlug(slug){const rows=await queryCollection('organizations',[["slug","==",slug]],null,1);return rows[0]||null;},async checkSlugAvailability(slug){const rows=await queryCollection('organizations',[["slug","==",slug]],null,1);return !rows.length;},deleteOrganization(id){return deleteDoc('organizations',id);},
  listUsers:loadUsers,async createUser(data){return callFunction('createUser',{payload:data}).catch(()=>createDoc('users',{...data,status:data.status||'pending_invite',inviteStatus:'pending_invite'}));},async createUserProfile(data){if(!data.uid&&!data.id){return callFunction('createUser',{payload:data}).catch(()=>callFunction('createCompanyUser',{payload:data}));}return updateDoc('users',data.uid||data.id,{...data,uid:data.uid||data.id});},repairUserProfile(uid,profile={}){return callFunction('repairUserProfile',{uid,profile});},updateUserProfile(id,data){return updateDoc('users',id,data);},deactivateUser(id){return updateDoc('users',id,{status:'inactive'});},reactivateUser(id){return updateDoc('users',id,{status:'active'});},
  listPlans(){return queryCollection('plans');},createPlan(data){return createDoc('plans',data);},updatePlan(id,data){return updateDoc('plans',id,data);},deletePlan(id){return deleteDoc('plans',id);},
  listModules(){return queryCollection('modules');},createModule(data){return createDoc('modules',data);},updateModule(idOrData,data){const id=typeof idOrData==='object'?idOrData.id:idOrData;return updateDoc('modules',id,data||idOrData);},
  listForms:loadForms,createForm(data){return createDoc('forms',data);},updateForm(id,data){return updateDoc('forms',id,data);},deleteForm(id){return deleteDoc('forms',id);},
  listSurveys:loadSurveys,createSurvey(data){return createDoc('surveys',data);},updateSurvey(id,data){return updateDoc('surveys',id,data);},deleteSurvey(id){return deleteDoc('surveys',id);},
  listResponses:loadResponses,getResponse(id){return getDoc('responses',id);},
  listInvitations(companyId){return scopedRows('invitations',companyId);},createInvitation(data){return createDoc('invitations',data);},updateInvitation(id,data){return updateDoc('invitations',id,data);},
  listActionPlans(companyId,filters={}){const fs=buildScopeFilter(companyId);Object.entries(filters||{}).forEach(([k,v])=>{if(v)fs.push([k,'==',v]);});return queryCollection('actionPlans',fs);},createActionPlan(data){return createDoc('actionPlans',data);},updateActionPlan(id,data){return updateDoc('actionPlans',id,data);},deleteActionPlan(id){return deleteDoc('actionPlans',id);},addActionComment(actionId,comment){return updateDoc('actionPlans',actionId,{comments:fv().arrayUnion(comment)});},markActionCompleted(actionId){return updateDoc('actionPlans',actionId,{status:'completed',progress:100,completedAt:ts()});},generateActionPlansFromSurvey(surveyId){return queryCollection('actionPlans',[["surveyId","==",surveyId]]);},
  listNotifications(filters={}){const fs=[];Object.entries(filters||{}).forEach(([k,v])=>{if(v)fs.push([k,'==',v]);});return scopedRows('notifications').then(rows=>fs.length?rows.filter(r=>fs.every(([k,,v])=>String(r[k]||'')===String(v))):rows);},listUserNotifications(userId){return queryCollection('notifications',[["userId","==",userId]]);},listCompanyNotifications(companyId){return scopedRows('notifications',companyId);},createNotification(data){return createDoc('notifications',data);},updateNotification(id,data){return updateDoc('notifications',id,data);},markNotificationRead(id){return updateDoc('notifications',id,{read:true,readAt:ts()});},dismissNotification(id){return updateDoc('notifications',id,{dismissed:true,dismissedAt:ts()});},markAllNotificationsRead(userId){return queryCollection('notifications',[["userId","==",userId]]).then(rows=>Promise.all(rows.map(n=>updateDoc('notifications',n.id,{read:true,readAt:ts()}))));},
  listIntegrations(companyId){return scopedRows('integrations',companyId);},createIntegration(data){return createDoc('integrations',data);},updateIntegration(id,data){return updateDoc('integrations',id,data);},deleteIntegration(id){return deleteDoc('integrations',id);},listApiKeys(companyId){return scopedRows('apiKeys',companyId).then(rows=>rows.map(x=>({...x,keyHash:x.keyHash?'[protected]':''})));},createApiKey(data){return callFunction('createApiKey',data);},revokeApiKey(id){return callFunction('revokeApiKey',{id});},listWebhooks(companyId){return scopedRows('webhooks',companyId).then(rows=>rows.map(x=>({...x,secretHash:x.secretHash?'[protected]':''})));},createWebhook(data){return createDoc('webhooks',data);},updateWebhook(id,data){return updateDoc('webhooks',id,data);},deleteWebhook(id){return deleteDoc('webhooks',id);},testWebhook(id){return callFunction('testWebhook',{id});},importEmployees(rows){return callFunction('importEmployees',{rows});},exportData(type,filters){return callFunction('exportData',{type,filters});},
  listInvoices(companyId,filters={}){return scopedRows('invoices',companyId).then(rows=>rows.filter(x=>Object.entries(filters||{}).every(([k,v])=>!v||String(x[k]||'')===String(v))));},getInvoice(id){return getDoc('invoices',id);},createInvoice(data){return createDoc('invoices',data);},updateInvoice(id,data){return updateDoc('invoices',id,data);},markInvoicePaid(id){return updateDoc('invoices',id,{status:'paid',paidAt:new Date().toISOString(),updatedAt:new Date().toISOString()});},cancelInvoice(id){return updateDoc('invoices',id,{status:'cancelled',cancelledAt:new Date().toISOString(),updatedAt:new Date().toISOString()});},getSubscription(companyId){return getDoc('organizations',companyId).then(c=>c?.subscription||null);},updateSubscription(companyId,data){return updateDoc('organizations',companyId,{subscription:data});},createPaymentLink(invoiceId){return callFunction('createPaymentLink',{invoiceId});},requestPlanUpgrade(companyId,planId){return callFunction('requestPlanUpgrade',{companyId,planId});},
  async saveChanges({state}={}){session.store=state||session.store;if(!session.profile||!session.loaded)return session.store;try{await Promise.all(['companies','users','plans','modules','forms','surveys','invitations','invoices','actionPlans','supportTickets','supportSlaPolicies','supportCategories','knowledgeBase','notifications','integrations','apiKeys','webhooks','integrationLogs','communications','units','serviceDeliverables'].map(syncCollectionFromState));await syncSettings();}catch(err){console.warn('[Valora Pulse] Falha ao salvar no Firestore.',err);throw mapCollectionError(err);}return session.store;},
  async saveAuditLog(){console.info('[Valora Pulse] Auditoria direta pelo frontend não é permitida pelas rules; use Cloud Function/Admin SDK.');return false;}
};
})();
