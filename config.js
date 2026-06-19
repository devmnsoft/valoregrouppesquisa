(function(){
'use strict';

// Configuração central do Valora Pulse™.
// Em produção, altere STORAGE_MODE para "firebase" somente depois de configurar
// Firebase Auth, Firestore, Cloud Functions e regras de segurança revisadas.
window.ValoraConfig=Object.freeze({
  APP_VERSION:'8.3.0',
  STORAGE_MODE:'local', // "local" para demo/MVP; "firebase" para produção.
  FIREBASE_ENABLED:false,
  REQUIRE_AUTH_SERVER_VALIDATION:false,
  STORE_KEY:'valoraPulseFinal800'
});
})();
