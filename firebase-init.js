(function(){
'use strict';

const config=window.ValoraConfig||{};
const firebaseConfig={...(config.FIREBASE_CONFIG||{})};

function hasFirebaseConfig(cfg){return !!(cfg.apiKey&&cfg.authDomain&&cfg.projectId&&cfg.appId);}
function compat(){return window.firebase&&typeof window.firebase.initializeApp==='function';}

let services={app:null,auth:null,db:null,initialized:false,available:false,error:null};

try{
  if(config.STORAGE_MODE==='firebase'&&config.FIREBASE_ENABLED&&hasFirebaseConfig(firebaseConfig)&&compat()){
    services.app=window.firebase.apps?.length?window.firebase.app():window.firebase.initializeApp(firebaseConfig);
    services.auth=window.firebase.auth();
    services.db=window.firebase.firestore();
    services.initialized=true;
    services.available=true;
    try{services.auth.setPersistence(window.firebase.auth.Auth.Persistence.LOCAL);}catch(err){console.warn('[Valora Pulse] Persistência Firebase Auth não configurada.',err);}
  }else if(config.STORAGE_MODE==='firebase'){
    services.error='Firebase não configurado. Confira FIREBASE_ENABLED, FIREBASE_CONFIG e SDKs no index.html.';
    console.warn(`[Valora Pulse] ${services.error}`);
  }
}catch(err){
  services.error=err?.message||String(err);
  console.error('[Valora Pulse] Falha ao inicializar Firebase.',err);
}

window.ValoraFirebaseConfig=firebaseConfig;
window.ValoraFirebaseServices=services;
})();
