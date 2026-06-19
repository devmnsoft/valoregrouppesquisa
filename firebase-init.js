(function(){
'use strict';

// Inicialização Firebase — estrutura intencionalmente sem credenciais reais.
// Próximos passos:
// 1. Incluir SDK modular/compat do Firebase no index.html ou via bundler.
// 2. Preencher firebaseConfig com variáveis do ambiente/projeto autorizado.
// 3. Inicializar Firebase App, Auth e Firestore.
// 4. Expor instâncias em window.ValoraFirebaseServices.
// 5. Validar permissões em Firestore Rules e ações sensíveis em Cloud Functions.
const firebaseConfig={
  // apiKey:'...',
  // authDomain:'...',
  // projectId:'...',
  // storageBucket:'...',
  // messagingSenderId:'...',
  // appId:'...'
};

window.ValoraFirebaseConfig=firebaseConfig;
window.ValoraFirebaseServices=window.ValoraFirebaseServices||{
  app:null,
  auth:null,
  db:null,
  initialized:false
};
})();
