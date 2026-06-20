(function(){
'use strict';

// Configuração central do Valora Pulse™.
// Em produção, altere STORAGE_MODE para "firebase" somente depois de configurar
// Firebase Auth, Firestore, Cloud Functions e regras de segurança revisadas.
window.ValoraConfig=Object.freeze({
  APP_VERSION:'8.6.3',
  STORAGE_MODE:'local', // "local" para demo/MVP; "firebase" para produção.
  FIREBASE_ENABLED:false,
  // Configuração pública do app Web Firebase. Preencha somente chaves públicas do projeto.
  FIREBASE_CONFIG:{
    // apiKey:'',
    // authDomain:'',
    // projectId:'',
    // storageBucket:'',
    // messagingSenderId:'',
    // appId:''
  },
  REQUIRE_AUTH_SERVER_VALIDATION:false,
  STORE_KEY:'valoraPulseFinal800'
});
})();
