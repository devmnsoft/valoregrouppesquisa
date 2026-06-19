(function(){
'use strict';

function clone(v){return JSON.parse(JSON.stringify(v));}

window.ValoraLocalRepository={
  mode:'local',
  loadStore({storeKey,seedStore,normalizeState}){
    try{
      const raw=localStorage.getItem(storeKey);
      if(raw){const obj=JSON.parse(raw);normalizeState(obj);return obj;}
    }catch(err){console.warn('[Valora Pulse] Falha ao carregar localStorage',err);}
    const seeded=seedStore();
    localStorage.setItem(storeKey,JSON.stringify(seeded));
    return seeded;
  },
  saveStore({storeKey,state}){localStorage.setItem(storeKey,JSON.stringify(state));},
  login({state,email,password,nowIso}){
    const user=state.users.find(x=>x.email.toLowerCase()===email.toLowerCase()&&x.password===password&&x.status==='active');
    if(!user)return null;
    state.session={userId:user.id,createdAt:nowIso()};
    return user;
  },
  logout({state}){state.session=null;},
  currentUser({state}){return state.users.find(u=>u.id===state.session?.userId)||null;},
  loadCompanies({state}){return state.companies;},
  loadUsers({state}){return state.users;},
  loadForms({state}){return state.forms;},
  loadSurveys({state}){return state.surveys;},
  loadResponses({state}){return state.responses;},
  saveChanges({storeKey,state}){this.saveStore({storeKey,state});return clone(state);}
};
})();
