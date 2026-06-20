(function(){
'use strict';

const ALLOWED_ROLES=Object.keys(window.ValoraRoles?.ROLE_DEFINITIONS||{admin_valora:1,consultor_valora:1,empresa_admin:1,gestor_pesquisa:1,analista_resultados:1,gestor_area:1,participante:1,convidado_externo:1});
const GLOBAL_ROLES=['admin_valora','consultor_valora'];
const PROFILE_MISSING_MESSAGE='Seu usuário ainda não possui perfil configurado. Solicite liberação ao administrador.';
const FRIENDLY_LOAD_ERROR='Não foi possível carregar as informações. Tente novamente.';
const session={authUser:null,profile:null,claims:{},ready:false,readyPromise:null,unsubscribe:null,store:null,normalizeState:null,loaded:false,loading:false,lastError:null,cache:{}};

function services(){return window.ValoraFirebaseServices||{};}
function ensureFirebase(){const s=services();if(!s.initialized||!s.auth||!s.db)throw Object.assign(new Error('Serviço de autenticação indisponível. Tente novamente mais tarde.'),{code:'network'});return s;}
function firestore(){return ensureFirebase().db;}
function fv(){return window.firebase.firestore.FieldValue;}
function ts(){return fv().serverTimestamp();}
function clone(v){return JSON.parse(JSON.stringify(v||null));}
function publicProfile(doc,claims={}){if(!doc)return null;const role=ALLOWED_ROLES.includes(claims.role)?claims.role:doc.role;return {id:doc.uid||doc.id,uid:doc.uid||doc.id,name:doc.name||doc.email,email:doc.email,phone:doc.phone||'',position:doc.position||'',department:doc.department||'',role,companyId:doc.companyId||claims.companyId||'',status:doc.status||'inactive',preferences:doc.preferences||{},claims};}
function cleanFirebaseLocalState(){try{Object.keys(localStorage).filter(k=>k.startsWith('firebase:')||k.includes('ValoraFirebase')).forEach(k=>localStorage.removeItem(k));}catch(_){} }
function docData(d){return d.exists?{id:d.id,...d.data()}:null;}
function asArray(snap){return snap.docs.map(docData).filter(Boolean);}
function isGlobalRole(profile=session.profile){return GLOBAL_ROLES.includes(profile?.role);}
function actorId(){return session.profile?.uid||session.profile?.id||session.authUser?.uid||'system';}
function normalizeCompany(org){return org?{...org,id:org.id||org.uid}:org;}
function collectionNameForStateKey(key){return ({companies:'organizations'})[key]||key;}
function collectionStateKey(collection){return ({organizations:'companies'})[collection]||collection;}
function canUseCompanyScope(){return !!session.profile?.companyId&&!isGlobalRole();}
function buildScopeFilter(companyId){return companyId?[['companyId','==',companyId]]:[];}
function mapCollectionError(err){
  const code=err?.code||'';
  if(code==='permission-denied')return Object.assign(new Error('Seu perfil não possui permissão para acessar estes dados.'),{code});
  if(code==='unauthenticated'||code==='auth/user-token-expired')return Object.assign(new Error('Sua sessão expirou. Entre novamente.'),{code});
  if(code==='unavailable'||code.includes('network'))return Object.assign(new Error('Não foi possível carregar as informações. Tente novamente.'),{code});
  return Object.assign(new Error(FRIENDLY_LOAD_ERROR),{code});
}
async function getClaims(user=session.authUser){if(!user)return {};const token=await user.getIdTokenResult(true);return token.claims||{};}
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
function emptyStore(seedStore,normalizeState){const state=seedStore();Object.assign(state,{session:null,users:[],companies:[],forms:[],surveys:[],responses:[],invitations:[],invoices:[],actionPlans:[],logs:[]});normalizeState(state);return state;}
async function queryCollection(name,filters=[],orderBy=null,limit=null){
  try{let q=firestore().collection(name);filters.forEach(([field,op,value])=>{if(value!==undefined&&value!==null&&value!=='')q=q.where(field,op,value);});if(orderBy)q=q.orderBy(orderBy);if(limit)q=q.limit(limit);return asArray(await q.get());}
  catch(err){throw mapCollectionError(err);}
}
async function getDoc(name,id){try{return docData(await firestore().collection(name).doc(id).get());}catch(err){throw mapCollectionError(err);}}
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
async function loadStoreData(){
  const p=session.profile;if(!p)return null;
  const canFinance=['admin_valora','empresa_admin'].includes(p.role);
  const [organizations,users,plans,modules,forms,surveys,responses,invitations,invoices,actionPlans,settings,logs]=await Promise.all([
    loadOrganizations(),loadUsers(),queryCollection('plans'),queryCollection('modules'),loadForms(),loadSurveys(),loadResponses(),scopedRows('invitations'),canFinance?scopedRows('invoices'):Promise.resolve([]),scopedRows('actionPlans'),loadSettings(),p.role==='admin_valora'?queryCollection('logs',[],null,300):Promise.resolve([])
  ]);
  return {session:{userId:p.id||p.uid,createdAt:new Date().toISOString()},companies:organizations.map(normalizeCompany),organizations,users,plans,modules,forms,surveys,responses,invitations,invoices,actionPlans,settings,logs};
}
async function hydrateStore(){
  if(session.loading)return;session.loading=true;session.lastError=null;
  try{const data=await loadStoreData();if(!data)return;Object.assign(session.store,data);session.normalizeState?.(session.store);session.cache=clone({companies:session.store.companies,users:session.store.users,plans:session.store.plans,modules:session.store.modules,forms:session.store.forms,surveys:session.store.surveys,responses:session.store.responses,invitations:session.store.invitations,invoices:session.store.invoices,actionPlans:session.store.actionPlans,settings:session.store.settings});session.loaded=true;}
  catch(err){session.lastError=err;console.warn('[Valora Pulse] Falha ao carregar Firestore.',err);}
  finally{session.loading=false;}
}
function changed(a,b){return JSON.stringify(a||null)!==JSON.stringify(b||null);}
async function syncCollectionFromState(key){const name=collectionNameForStateKey(key);const before=new Map((session.cache[key]||[]).map(x=>[x.id,x]));const now=new Map((session.store[key]||[]).map(x=>[x.id,x]));const writes=[];now.forEach((row,id)=>{if(key==='users'&&(!row.uid||row.uid!==id)&&!before.has(id)){console.warn('[Valora Pulse] TODO seguro: criação de usuário Firebase Auth deve ocorrer via Cloud Function/Admin SDK antes de gravar users/{uid}.',row.email);return;}if(!before.has(id))writes.push(createDoc(name,row));else if(changed(row,before.get(id)))writes.push(updateDoc(name,id,row));});before.forEach((_row,id)=>{if(!now.has(id))writes.push(deleteDoc(name,id));});await Promise.all(writes);session.cache[key]=clone(session.store[key]||[]);}
async function syncSettings(){if(!changed(session.store.settings,session.cache.settings))return;const value=session.store.settings||{};if(value.public||Object.keys(value).some(k=>typeof value[k]==='object'))await Promise.all(Object.entries(value).map(([id,data])=>updateDoc('settings',id,data)));else await updateDoc('settings','public',value);session.cache.settings=clone(value);}
function mapAuthError(err){const code=err?.code||'';if(['auth/invalid-credential','auth/user-not-found','auth/wrong-password'].includes(code))return 'Credenciais inválidas. Confira e-mail e senha.';if(code==='inactive-user')return 'Usuário inativo. Solicite liberação ao administrador.';if(code==='profile-missing')return PROFILE_MISSING_MESSAGE;if(code==='inactive-company')return 'Sua empresa está inativa. Contate o administrador.';if(code.includes('network')||code==='unavailable')return 'Não foi possível conectar ao serviço. Verifique sua internet e tente novamente.';return 'Não foi possível entrar agora. Tente novamente em instantes.';}

window.ValoraFirebaseAuth={getCurrentAuthUser,getCurrentUserProfile,waitUntilReady};
window.ValoraFirebaseRepository={
  mode:'firebase',
  loadStore({seedStore,normalizeState}){cleanFirebaseLocalState();session.store=emptyStore(seedStore,normalizeState);session.normalizeState=normalizeState;waitUntilReady().catch(()=>{});return session.store;},
  saveStore(){return this.saveChanges({state:session.store});},
  async login({email,password}){const s=ensureFirebase();try{const cred=await s.auth.signInWithEmailAndPassword(email,password);const profile=await loadProfile(cred.user);await hydrateStore();return profile;}catch(err){throw Object.assign(new Error(mapAuthError(err)),{code:err?.code});}},
  async logout(){const s=ensureFirebase();session.profile=null;session.claims={};session.loaded=false;cleanFirebaseLocalState();await s.auth.signOut();},
  currentUser(){return session.profile;},
  async loadStoreFromFirestore(){await hydrateStore();return session.store;},
  loadCompanies({state}={}){return state?.companies||[];},loadOrganizations:loadOrganizations,loadUsers({state}={}){return state?.users||[];},loadPlans({state}={}){return state?.plans||[];},loadModules({state}={}){return state?.modules||[];},loadForms({state}={}){return state?.forms||[];},loadSurveys({state}={}){return state?.surveys||[];},loadResponses({state}={}){return state?.responses||[];},loadInvitations({state}={}){return state?.invitations||[];},loadActionPlans({state}={}){return state?.actionPlans||[];},loadInvoices({state}={}){return state?.invoices||[];},loadSettings({state}={}){return state?.settings||{};},
  listOrganizations:loadOrganizations,getOrganization(id){return getDoc('organizations',id);},createOrganization(data){return createDoc('organizations',data);},updateOrganization(id,data){return updateDoc('organizations',id,data);},deleteOrganization(id){return deleteDoc('organizations',id);},
  listUsers:loadUsers,createUserProfile(data){return createDoc('users',{...data,uid:data.uid||data.id});},updateUserProfile(id,data){return updateDoc('users',id,data);},deactivateUser(id){return updateDoc('users',id,{status:'inactive'});},reactivateUser(id){return updateDoc('users',id,{status:'active'});},
  listPlans(){return queryCollection('plans');},createPlan(data){return createDoc('plans',data);},updatePlan(id,data){return updateDoc('plans',id,data);},deletePlan(id){return deleteDoc('plans',id);},
  listModules(){return queryCollection('modules');},createModule(data){return createDoc('modules',data);},updateModule(idOrData,data){const id=typeof idOrData==='object'?idOrData.id:idOrData;return updateDoc('modules',id,data||idOrData);},
  listForms:loadForms,createForm(data){return createDoc('forms',data);},updateForm(id,data){return updateDoc('forms',id,data);},deleteForm(id){return deleteDoc('forms',id);},
  listSurveys:loadSurveys,createSurvey(data){return createDoc('surveys',data);},updateSurvey(id,data){return updateDoc('surveys',id,data);},deleteSurvey(id){return deleteDoc('surveys',id);},
  listResponses:loadResponses,getResponse(id){return getDoc('responses',id);},
  listInvitations(companyId){return scopedRows('invitations',companyId);},createInvitation(data){return createDoc('invitations',data);},updateInvitation(id,data){return updateDoc('invitations',id,data);},
  listActionPlans(companyId,filters={}){const fs=buildScopeFilter(companyId);Object.entries(filters||{}).forEach(([k,v])=>{if(v)fs.push([k,'==',v]);});return queryCollection('actionPlans',fs);},createActionPlan(data){return createDoc('actionPlans',data);},updateActionPlan(id,data){return updateDoc('actionPlans',id,data);},deleteActionPlan(id){return deleteDoc('actionPlans',id);},addActionComment(actionId,comment){return updateDoc('actionPlans',actionId,{comments:fv().arrayUnion(comment)});},markActionCompleted(actionId){return updateDoc('actionPlans',actionId,{status:'completed',progress:100,completedAt:ts()});},generateActionPlansFromSurvey(surveyId){return queryCollection('actionPlans',[["surveyId","==",surveyId]]);},
  listInvoices(companyId){return scopedRows('invoices',companyId);},createInvoice(data){return createDoc('invoices',data);},updateInvoice(id,data){return updateDoc('invoices',id,data);},
  async saveChanges({state}={}){session.store=state||session.store;if(!session.profile||!session.loaded)return session.store;try{await Promise.all(['companies','users','plans','modules','forms','surveys','invitations','invoices','actionPlans'].map(syncCollectionFromState));await syncSettings();}catch(err){console.warn('[Valora Pulse] Falha ao salvar no Firestore.',err);throw mapCollectionError(err);}return session.store;},
  async saveAuditLog(){console.info('[Valora Pulse] Auditoria direta pelo frontend não é permitida pelas rules; use Cloud Function/Admin SDK.');return false;}
};
})();
