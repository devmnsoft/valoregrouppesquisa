(function(){
'use strict';

const config=window.ValoraConfig||{};
const firebaseConfig=config.FIREBASE_CONFIG||{};

function hasFirebaseConfig(cfg){return !!(cfg.apiKey&&cfg.authDomain&&cfg.projectId&&cfg.appId);}
function hasFirebaseSdk(){return !!(window.firebase&&typeof window.firebase.initializeApp==='function'&&typeof window.firebase.auth==='function'&&typeof window.firebase.firestore==='function');}

let services={app:null,auth:null,db:null,functions:null,initialized:false,available:false,error:null};

try{
  const needsFirebase=config.STORAGE_MODE==='firebase'&&config.FIREBASE_ENABLED===true;
  if(needsFirebase&&hasFirebaseConfig(firebaseConfig)&&hasFirebaseSdk()){
    services.app=window.firebase.apps?.length?window.firebase.app():window.firebase.initializeApp(firebaseConfig);
    services.auth=window.firebase.auth();
    services.db=window.firebase.firestore();
    services.functions=typeof window.firebase.functions==='function'?window.firebase.functions():null;
    services.initialized=true;
    services.available=true;
    try{services.auth.setPersistence(window.firebase.auth.Auth.Persistence.LOCAL);}catch(err){console.warn('[Valora Pulse] Persistência Firebase Auth não configurada.',err);}
  }else if(needsFirebase){
    const missing=[];
    if(!hasFirebaseSdk())missing.push('SDK compat Firebase');
    if(!hasFirebaseConfig(firebaseConfig))missing.push('FIREBASE_CONFIG com apiKey, authDomain, projectId e appId');
    services.error=`Firebase não configurado: ${missing.join(' e ')} ausente(s). Confira config.js, SDKs compat e ordem dos scripts no index.html.`;
    console.error(`[Valora Pulse] ${services.error}`);
  }
}catch(err){
  services.error=err?.message||String(err);
  console.error('[Valora Pulse] Falha ao inicializar Firebase.',err);
}

window.ValoraFirebaseConfig=firebaseConfig;
window.ValoraFirebaseServices=services;
})();
