(function(){
'use strict';

// Configuração central do Valora Pulse™.
// Em produção, altere STORAGE_MODE para "firebase" somente depois de configurar
// Firebase Auth, Firestore, Cloud Functions e regras de segurança revisadas.
window.ValoraConfig=Object.freeze({
  APP_VERSION:'8.6.8',
  STORAGE_MODE:'firebase', // "local" para demo/MVP; "firebase" para produção.
  FIREBASE_ENABLED:true,
  observability:{enabled:true,consoleEnabled:true,persistLogs:true,telegramEnabled:false,telegramLevels:['critical','error','security'],telegramCategories:['system','security','billing','integration','firebase'],maskSensitiveData:true,maxLocalLogs:3000,environment:'local'},
  // Configuração pública do app Web Firebase. Preencha somente chaves públicas do projeto.
  FIREBASE_CONFIG:{
    projectId:'gestordepesquisa',
    authDomain:'gestordepesquisa.firebaseapp.com',
    storageBucket:'gestordepesquisa.appspot.com'
  },
  REQUIRE_AUTH_SERVER_VALIDATION:false,
  STORE_KEY:'valoraPulseFinal800'
});
})();
