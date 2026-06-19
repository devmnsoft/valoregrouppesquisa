(function(){
'use strict';

const config=window.ValoraConfig||{};
const storageMode=config.STORAGE_MODE==='firebase'?'firebase':'local';
const selected=storageMode==='firebase'?window.ValoraFirebaseRepository:window.ValoraLocalRepository;

if(!selected)throw new Error(`Repositório ${storageMode} não disponível.`);
if(storageMode==='firebase'&&!config.FIREBASE_ENABLED){
  console.warn('[Valora Pulse] STORAGE_MODE=firebase, mas FIREBASE_ENABLED=false. A integração Firebase está preparada, porém pendente de configuração.');
}

window.ValoraRepository=selected;
})();
