(function(){
'use strict';

const ALLOWED_ROLES=['admin_valora','consultor_valora','empresa_admin','gestor_pesquisa','participante'];
const PROFILE_MISSING_MESSAGE='Seu usuário ainda não possui perfil configurado. Solicite liberação ao administrador.';
const session={authUser:null,profile:null,claims:{},ready:false,readyPromise:null,unsubscribe:null};

function services(){return window.ValoraFirebaseServices||{};}
function ensureFirebase(){const s=services();if(!s.initialized||!s.auth||!s.db)throw Object.assign(new Error('Serviço de autenticação indisponível. Tente novamente mais tarde.'),{code:'network'});return s;}
function publicProfile(doc,claims={}){if(!doc)return null;const role=ALLOWED_ROLES.includes(claims.role)?claims.role:doc.role;return {id:doc.uid,uid:doc.uid,name:doc.name||doc.email,email:doc.email,phone:doc.phone||'',role,companyId:doc.companyId||claims.companyId||'',status:doc.status||'inactive',preferences:doc.preferences||{},claims};}
function cleanFirebaseLocalState(){try{Object.keys(localStorage).filter(k=>k.startsWith('firebase:')||k.includes('ValoraFirebase')).forEach(k=>localStorage.removeItem(k));}catch(_){}}
async function getClaims(user=session.authUser){if(!user)return {};const token=await user.getIdTokenResult(true);return token.claims||{};}
async function getCurrentAuthUser(){await waitUntilReady();return session.authUser;}
async function getCurrentUserProfile(){await waitUntilReady();return session.profile;}
async function loadProfile(user){
  if(!user)return null;
  const s=ensureFirebase();
  const claims=await getClaims(user);
  const ref=s.db.collection('users').doc(user.uid);
  const snap=await ref.get();
  if(!snap.exists)throw Object.assign(new Error(PROFILE_MISSING_MESSAGE),{code:'profile-missing'});
  const doc={uid:user.uid,...snap.data()};
  const profile=publicProfile(doc,claims);
  if(profile.status!=='active')throw Object.assign(new Error('Usuário inativo.'),{code:'inactive-user'});
  await ref.set({lastLoginAt:window.firebase.firestore.FieldValue.serverTimestamp(),updatedAt:doc.updatedAt||window.firebase.firestore.FieldValue.serverTimestamp()},{merge:true});
  session.claims=claims;session.profile=profile;return profile;
}
function waitUntilReady(){
  if(session.ready)return Promise.resolve(session.profile);
  if(session.readyPromise)return session.readyPromise;
  const s=ensureFirebase();
  session.readyPromise=new Promise(resolve=>{
    session.unsubscribe=s.auth.onAuthStateChanged(async user=>{
      session.authUser=user;session.profile=null;session.claims={};
      try{if(user)await loadProfile(user);}catch(err){console.warn('[Valora Pulse] Sessão bloqueada.',err);try{await s.auth.signOut();}catch(_){} }
      session.ready=true;resolve(session.profile);
      window.dispatchEvent(new CustomEvent('valora:auth-changed',{detail:{user:session.profile}}));
    });
  });
  return session.readyPromise;
}
function emptyStore(seedStore,normalizeState){const state=seedStore();state.session=null;state.users=[];normalizeState(state);return state;}
function mapAuthError(err){
  const code=err?.code||'';
  if(['auth/invalid-credential','auth/user-not-found','auth/wrong-password'].includes(code))return 'Credenciais inválidas. Confira e-mail e senha.';
  if(code==='inactive-user')return 'Usuário inativo. Solicite liberação ao administrador.';
  if(code==='profile-missing')return PROFILE_MISSING_MESSAGE;
  if(code.includes('network')||code==='unavailable')return 'Não foi possível conectar ao serviço. Verifique sua internet e tente novamente.';
  return 'Não foi possível entrar agora. Tente novamente em instantes.';
}

window.ValoraFirebaseAuth={getCurrentAuthUser,getCurrentUserProfile,waitUntilReady};
window.ValoraFirebaseRepository={
  mode:'firebase',
  loadStore({seedStore,normalizeState}){cleanFirebaseLocalState();waitUntilReady().catch(()=>{});return emptyStore(seedStore,normalizeState);},
  saveStore(){},
  async login({email,password}){const s=ensureFirebase();try{const cred=await s.auth.signInWithEmailAndPassword(email,password);return await loadProfile(cred.user);}catch(err){throw Object.assign(new Error(mapAuthError(err)),{code:err?.code});}},
  async logout(){const s=ensureFirebase();session.profile=null;session.claims={};cleanFirebaseLocalState();await s.auth.signOut();},
  currentUser(){return session.profile;},
  loadCompanies({state}){return state.companies;},loadUsers({state}){return state.users;},loadForms({state}){return state.forms;},loadSurveys({state}){return state.surveys;},loadResponses({state}){return state.responses;},
  saveChanges({state}){return state;}
};
})();
