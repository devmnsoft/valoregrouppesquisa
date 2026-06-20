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
  listOrganizations({state}={}){return Promise.resolve(clone(state?.companies||[]));},
  getOrganization(id,{state}={}){return Promise.resolve(clone((state?.companies||[]).find(x=>x.id===id)||null));},
  createOrganization(data,{state}={}){const item={id:data.id||`org_${Date.now().toString(36)}`,...data};state?.companies?.push(item);return Promise.resolve(clone(item));},
  updateOrganization(id,data,{state}={}){const item=(state?.companies||[]).find(x=>x.id===id);if(item)Object.assign(item,data,{updatedAt:data.updatedAt||new Date().toISOString()});return Promise.resolve(clone(item));},
  listUsers(companyId,{state}={}){const rows=state?.users||[];return Promise.resolve(clone(companyId?rows.filter(x=>x.companyId===companyId):rows));},
  createUserProfile(data,{state}={}){const item={id:data.id||`u_${Date.now().toString(36)}`,...data};state?.users?.push(item);return Promise.resolve(clone(item));},
  updateUserProfile(id,data,{state}={}){const item=(state?.users||[]).find(x=>x.id===id);if(item)Object.assign(item,data,{updatedAt:data.updatedAt||new Date().toISOString()});return Promise.resolve(clone(item));},
  listPlans({state}={}){return Promise.resolve(clone(state?.plans||[]));},
  createPlan(data,{state}={}){const item={id:data.id||`plan_${Date.now().toString(36)}`,...data};state?.plans?.push(item);return Promise.resolve(clone(item));},
  updatePlan(id,data,{state}={}){const item=(state?.plans||[]).find(x=>x.id===id);if(item)Object.assign(item,data,{updatedAt:data.updatedAt||new Date().toISOString()});return Promise.resolve(clone(item));},
  listModules({state}={}){return Promise.resolve(clone(state?.modules||[]));},
  updateModule(data,{state}={}){const item=(state?.modules||[]).find(x=>x.id===data.id);if(item)Object.assign(item,data);return Promise.resolve(clone(item));},
  listForms(companyId,{state}={}){const rows=state?.forms||[];return Promise.resolve(clone(companyId?rows.filter(x=>x.companyId===companyId):rows));},
  createForm(data,{state}={}){const item={id:data.id||`form_${Date.now().toString(36)}`,...data};state?.forms?.push(item);return Promise.resolve(clone(item));},
  updateForm(id,data,{state}={}){const item=(state?.forms||[]).find(x=>x.id===id);if(item)Object.assign(item,data,{updatedAt:data.updatedAt||new Date().toISOString()});return Promise.resolve(clone(item));},
  listSurveys(companyId,{state}={}){const rows=state?.surveys||[];return Promise.resolve(clone(companyId?rows.filter(x=>x.companyId===companyId):rows));},
  createSurvey(data,{state}={}){const item={id:data.id||`survey_${Date.now().toString(36)}`,...data};state?.surveys?.push(item);return Promise.resolve(clone(item));},
  updateSurvey(id,data,{state}={}){const item=(state?.surveys||[]).find(x=>x.id===id);if(item)Object.assign(item,data,{updatedAt:data.updatedAt||new Date().toISOString()});return Promise.resolve(clone(item));},
  listResponses(companyId,{state}={}){const rows=state?.responses||[];return Promise.resolve(clone(companyId?rows.filter(x=>x.companyId===companyId):rows));},
  listInvitations(companyId,{state}={}){const rows=state?.invitations||[];return Promise.resolve(clone(companyId?rows.filter(x=>x.companyId===companyId):rows));},
  createInvitation(data,{state}={}){const item={id:data.id||`invite_${Date.now().toString(36)}`,...data};state?.invitations?.push(item);return Promise.resolve(clone(item));},
  updateInvitation(id,data,{state}={}){const item=(state?.invitations||[]).find(x=>x.id===id);if(item)Object.assign(item,data,{updatedAt:data.updatedAt||new Date().toISOString()});return Promise.resolve(clone(item));},
  loadCompanies({state}){return state.companies;},
  loadUsers({state}){return state.users;},
  loadForms({state}){return state.forms;},
  loadSurveys({state}){return state.surveys;},
  loadResponses({state}){return state.responses;},
  listNotifications(filters={}, {state}={}){let rows=state?.notifications||[];Object.entries(filters||{}).forEach(([k,v])=>{if(v)rows=rows.filter(x=>String(x[k]||'')===String(v));});return Promise.resolve(clone(rows));},
  listUserNotifications(userId,{state}={}){return Promise.resolve(clone((state?.notifications||[]).filter(x=>x.userId===userId)));},
  listCompanyNotifications(companyId,{state}={}){return Promise.resolve(clone((state?.notifications||[]).filter(x=>x.companyId===companyId)));},
  createNotification(data,{state}={}){const item={id:data.id||`ntf_${Date.now().toString(36)}`,...data};state?.notifications?.push(item);return Promise.resolve(clone(item));},
  updateNotification(id,data,{state}={}){const item=(state?.notifications||[]).find(x=>x.id===id);if(item)Object.assign(item,data,{updatedAt:data.updatedAt||new Date().toISOString()});return Promise.resolve(clone(item));},
  markNotificationRead(id,{state}={}){const item=(state?.notifications||[]).find(x=>x.id===id);if(item)Object.assign(item,{read:true,readAt:new Date().toISOString(),updatedAt:new Date().toISOString()});return Promise.resolve(clone(item));},
  dismissNotification(id,{state}={}){const item=(state?.notifications||[]).find(x=>x.id===id);if(item)Object.assign(item,{dismissed:true,dismissedAt:new Date().toISOString(),updatedAt:new Date().toISOString()});return Promise.resolve(clone(item));},
  markAllNotificationsRead(userId,{state}={}){(state?.notifications||[]).filter(x=>!x.userId||x.userId===userId).forEach(x=>Object.assign(x,{read:true,readAt:new Date().toISOString(),updatedAt:new Date().toISOString()}));return Promise.resolve(true);},
  listActionPlans(companyId,{state,filters}={}){let rows=state?.actionPlans||[];if(companyId)rows=rows.filter(x=>x.companyId===companyId);if(filters)Object.entries(filters).forEach(([k,v])=>{if(v)rows=rows.filter(x=>String(x[k]||'')===String(v));});return Promise.resolve(clone(rows));},
  createActionPlan(data,{state}={}){const item={id:data.id||`act_${Date.now().toString(36)}`,...data};state?.actionPlans?.push(item);return Promise.resolve(clone(item));},
  updateActionPlan(id,data,{state}={}){const item=(state?.actionPlans||[]).find(x=>x.id===id);if(item)Object.assign(item,data,{updatedAt:data.updatedAt||new Date().toISOString()});return Promise.resolve(clone(item));},
  deleteActionPlan(id,{state}={}){if(state)state.actionPlans=(state.actionPlans||[]).filter(x=>x.id!==id);return Promise.resolve(true);},
  addActionComment(actionId,comment,{state}={}){const item=(state?.actionPlans||[]).find(x=>x.id===actionId);if(item)(item.comments||(item.comments=[])).push(comment);return Promise.resolve(clone(item));},
  markActionCompleted(actionId,{state}={}){const item=(state?.actionPlans||[]).find(x=>x.id===actionId);if(item)Object.assign(item,{status:'completed',progress:100,completedAt:new Date().toISOString()});return Promise.resolve(clone(item));},
  generateActionPlansFromSurvey(surveyId,{state}={}){return Promise.resolve(clone((state?.actionPlans||[]).filter(x=>x.surveyId===surveyId)));},
  saveChanges({storeKey,state}){this.saveStore({storeKey,state});return clone(state);}
};
})();
