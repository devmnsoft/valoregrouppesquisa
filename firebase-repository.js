(function(){
'use strict';

function notReady(method){
  throw new Error(`FirebaseRepository.${method} ainda não foi implementado. Configure Firebase Auth/Firestore antes de usar STORAGE_MODE=firebase.`);
}

window.ValoraFirebaseRepository={
  mode:'firebase',
  loadStore(){notReady('loadStore');},
  saveStore(){notReady('saveStore');},
  login(){notReady('login');},
  logout(){notReady('logout');},
  currentUser(){notReady('currentUser');},
  loadCompanies(){notReady('loadCompanies');},
  loadUsers(){notReady('loadUsers');},
  loadForms(){notReady('loadForms');},
  loadSurveys(){notReady('loadSurveys');},
  loadResponses(){notReady('loadResponses');},
  saveChanges(){notReady('saveChanges');}
};
})();
