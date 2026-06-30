(function(){
'use strict';

function clone(v){return JSON.parse(JSON.stringify(v));}

window.ValoraLocalRepository={
  mode:'local',
  loadStore({storeKey,seedStore,normalizeState}){
    const resetCorruptedLocalStore=reason=>{
      console.warn('[Valora Pulse] Base local incompatível. Recriando seed.',reason);
      try{localStorage.removeItem(storeKey);}catch(_){ }
      const seeded=seedStore();
      normalizeState(seeded);
      try{localStorage.setItem(storeKey,JSON.stringify(seeded));}catch(err){console.warn('[Valora Pulse] Não foi possível salvar seed local.',err);}
      return seeded;
    };
    try{
      const raw=localStorage.getItem(storeKey);
      if(raw){const obj=JSON.parse(raw);normalizeState(obj);return obj;}
    }catch(err){return resetCorruptedLocalStore(err);}
    return resetCorruptedLocalStore('Base local ausente.');
  },
  saveStore({storeKey,state}={}){if(!state)return;try{localStorage.setItem(storeKey,JSON.stringify(state));}catch(err){console.warn('[Valora Pulse] Não foi possível salvar base local.',err);}},
  login({state,email,password,nowIso}={}){
    const user=(state?.users||[]).find(x=>x.email.toLowerCase()===email.toLowerCase()&&x.password===password&&x.status==='active');
    if(!user)return null;
    if(state)state.session={userId:user.id,createdAt:nowIso()};
    return user;
  },
  logout({state}={}){if(state)state.session=null;},
  currentUser({state}={}){return (state?.users||[]).find(u=>u.id===state?.session?.userId)||null;},
  listOrganizations({state}={}){return Promise.resolve(clone(state?.companies||[]));},
  getOrganization(id,{state}={}){return Promise.resolve(clone((state?.companies||[]).find(x=>x.id===id)||null));},
  createOrganization(data,{state}={}){const item={id:data.id||`org_${Date.now().toString(36)}`,...data};state?.companies?.push(item);return Promise.resolve(clone(item));},
  registerCompany(data,{state}={}){const item={id:data.id||`org_${Date.now().toString(36)}`,type:data.type||'juridica',name:data.companyName||data.name||data.legalName||'',publicName:data.publicName||data.companyName||data.name||'',slug:data.slug||'',planId:data.planId||'free',status:'active',...data};state?.companies?.push(item);return Promise.resolve({ok:true,id:item.id,companyId:item.id,organizationId:item.id,slug:item.slug||'',status:'active',profile:item});},
  updateOrganization(id,data,{state}={}){const item=(state?.companies||[]).find(x=>x.id===id);if(item)Object.assign(item,data,{updatedAt:data.updatedAt||new Date().toISOString()});return Promise.resolve(clone(item));},updateOrganizationBrand(id,brand,{state}={}){return this.updateOrganization(id,{brand},{state});},updateOrganizationSubscription(id,subscription,{state}={}){return this.updateOrganization(id,{subscription,planId:subscription?.planId},{state});},updateOrganizationSettings(id,settings,{state}={}){return this.updateOrganization(id,{settings},{state});},updateOrganizationLimits(id,limitsOverride,{state}={}){return this.updateOrganization(id,{limitsOverride},{state});},getOrganizationBySlug(slug,{state}={}){return Promise.resolve(clone((state?.companies||[]).find(x=>x.slug===slug)||null));},checkSlugAvailability(slug,{state}={}){return Promise.resolve(!(state?.companies||[]).some(x=>x.slug===slug));},
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
  loadCompanies({state}={}){return state?.companies||[];},
  loadUsers({state}={}){return state?.users||[];},
  loadForms({state}={}){return state?.forms||[];},
  loadSurveys({state}={}){return state?.surveys||[];},
  loadResponses({state}={}){return state?.responses||[];},
  listNotifications(filters={}, {state}={}){let rows=state?.notifications||[];Object.entries(filters||{}).forEach(([k,v])=>{if(v)rows=rows.filter(x=>String(x[k]||'')===String(v));});return Promise.resolve(clone(rows));},
  listUserNotifications(userId,{state}={}){return Promise.resolve(clone((state?.notifications||[]).filter(x=>x.userId===userId)));},
  listCompanyNotifications(companyId,{state}={}){return Promise.resolve(clone((state?.notifications||[]).filter(x=>x.companyId===companyId)));},
  createNotification(data,{state}={}){const item={id:data.id||`ntf_${Date.now().toString(36)}`,...data};state?.notifications?.push(item);return Promise.resolve(clone(item));},
  updateNotification(id,data,{state}={}){const item=(state?.notifications||[]).find(x=>x.id===id);if(item)Object.assign(item,data,{updatedAt:data.updatedAt||new Date().toISOString()});return Promise.resolve(clone(item));},
  markNotificationRead(id,{state}={}){const item=(state?.notifications||[]).find(x=>x.id===id);if(item)Object.assign(item,{read:true,readAt:new Date().toISOString(),updatedAt:new Date().toISOString()});return Promise.resolve(clone(item));},
  dismissNotification(id,{state}={}){const item=(state?.notifications||[]).find(x=>x.id===id);if(item)Object.assign(item,{dismissed:true,dismissedAt:new Date().toISOString(),updatedAt:new Date().toISOString()});return Promise.resolve(clone(item));},
  markAllNotificationsRead(userId,{state}={}){(state?.notifications||[]).filter(x=>!x.userId||x.userId===userId).forEach(x=>Object.assign(x,{read:true,readAt:new Date().toISOString(),updatedAt:new Date().toISOString()}));return Promise.resolve(true);},

  listInvoices(companyId, filters={}, {state}={}){let rows=state?.invoices||[];if(companyId)rows=rows.filter(x=>x.companyId===companyId||x.organizationId===companyId);Object.entries(filters||{}).forEach(([k,v])=>{if(v)rows=rows.filter(x=>String(x[k]||'')===String(v));});return Promise.resolve(clone(rows));},
  getInvoice(id,{state}={}){return Promise.resolve(clone((state?.invoices||[]).find(x=>x.id===id)||null));},
  createInvoice(data,{state}={}){const item={id:data.id||`inv_${Date.now().toString(36)}`,...data};state?.invoices?.push(item);return Promise.resolve(clone(item));},
  updateInvoice(id,data,{state}={}){const item=(state?.invoices||[]).find(x=>x.id===id);if(item)Object.assign(item,data,{updatedAt:data.updatedAt||new Date().toISOString()});return Promise.resolve(clone(item));},
  markInvoicePaid(id,{state}={}){const item=(state?.invoices||[]).find(x=>x.id===id);if(item)Object.assign(item,{status:'paid',paidAt:new Date().toISOString(),updatedAt:new Date().toISOString()});return Promise.resolve(clone(item));},
  cancelInvoice(id,{state}={}){const item=(state?.invoices||[]).find(x=>x.id===id);if(item)Object.assign(item,{status:'cancelled',cancelledAt:new Date().toISOString(),updatedAt:new Date().toISOString()});return Promise.resolve(clone(item));},
  getSubscription(companyId,{state}={}){return Promise.resolve(clone((state?.companies||[]).find(x=>x.id===companyId)?.subscription||null));},
  updateSubscription(companyId,data,{state}={}){const item=(state?.companies||[]).find(x=>x.id===companyId);if(item)item.subscription={...(item.subscription||{}),...data,updatedAt:new Date().toISOString()};return Promise.resolve(clone(item?.subscription||null));},
  createPaymentLink(invoiceId,{state}={}){const item=(state?.invoices||[]).find(x=>x.id===invoiceId);if(item)item.paymentUrl=item.paymentUrl||`manual://invoice/${invoiceId}`;return Promise.resolve(clone({invoiceId,paymentUrl:item?.paymentUrl||''}));},
  requestPlanUpgrade(companyId,planId,{state}={}){return this.createNotification({companyId,type:'upgrade_recommended',severity:'info',title:'Solicitação de upgrade',message:`Plano solicitado: ${planId}`,createdAt:new Date().toISOString(),read:false},{state});},
  listActionPlans(companyId,{state,filters}={}){let rows=state?.actionPlans||[];if(companyId)rows=rows.filter(x=>x.companyId===companyId);if(filters)Object.entries(filters).forEach(([k,v])=>{if(v)rows=rows.filter(x=>String(x[k]||'')===String(v));});return Promise.resolve(clone(rows));},
  createActionPlan(data,{state}={}){const item={id:data.id||`act_${Date.now().toString(36)}`,...data};state?.actionPlans?.push(item);return Promise.resolve(clone(item));},
  updateActionPlan(id,data,{state}={}){const item=(state?.actionPlans||[]).find(x=>x.id===id);if(item)Object.assign(item,data,{updatedAt:data.updatedAt||new Date().toISOString()});return Promise.resolve(clone(item));},
  deleteActionPlan(id,{state}={}){if(state)state.actionPlans=(state.actionPlans||[]).filter(x=>x.id!==id);return Promise.resolve(true);},
  addActionComment(actionId,comment,{state}={}){const item=(state?.actionPlans||[]).find(x=>x.id===actionId);if(item)(item.comments||(item.comments=[])).push(comment);return Promise.resolve(clone(item));},
  markActionCompleted(actionId,{state}={}){const item=(state?.actionPlans||[]).find(x=>x.id===actionId);if(item)Object.assign(item,{status:'completed',progress:100,completedAt:new Date().toISOString()});return Promise.resolve(clone(item));},
  generateActionPlansFromSurvey(surveyId,{state}={}){return Promise.resolve(clone((state?.actionPlans||[]).filter(x=>x.surveyId===surveyId)));},

  listIntegrations(companyId,{state}={}){const rows=state?.integrations||[];return Promise.resolve(clone(companyId?rows.filter(x=>x.companyId===companyId):rows));},
  createIntegration(data,{state}={}){const item={id:data.id||`int_${Date.now().toString(36)}`,...data};(state.integrations||(state.integrations=[])).push(item);return Promise.resolve(clone(item));},
  updateIntegration(id,data,{state}={}){const item=(state?.integrations||[]).find(x=>x.id===id);if(item)Object.assign(item,data,{updatedAt:data.updatedAt||new Date().toISOString()});return Promise.resolve(clone(item));},
  deleteIntegration(id,{state}={}){if(state)state.integrations=(state.integrations||[]).filter(x=>x.id!==id);return Promise.resolve(true);},
  listApiKeys(companyId,{state}={}){const rows=state?.apiKeys||[];return Promise.resolve(clone(companyId?rows.filter(x=>x.companyId===companyId):rows).map(x=>({...x,keyHash:x.keyHash?'[protected]':''})));},
  createApiKey(data,{state}={}){const item={id:data.id||`key_${Date.now().toString(36)}`,...data};(state.apiKeys||(state.apiKeys=[])).push(item);return Promise.resolve(clone(item));},
  revokeApiKey(id,{state}={}){const item=(state?.apiKeys||[]).find(x=>x.id===id);if(item)Object.assign(item,{status:'revoked',revokedAt:new Date().toISOString()});return Promise.resolve(clone(item));},
  listWebhooks(companyId,{state}={}){const rows=state?.webhooks||[];return Promise.resolve(clone(companyId?rows.filter(x=>x.companyId===companyId):rows).map(x=>({...x,secretHash:x.secretHash?'[protected]':''})));},
  createWebhook(data,{state}={}){const item={id:data.id||`wh_${Date.now().toString(36)}`,...data};(state.webhooks||(state.webhooks=[])).push(item);return Promise.resolve(clone(item));},
  updateWebhook(id,data,{state}={}){const item=(state?.webhooks||[]).find(x=>x.id===id);if(item)Object.assign(item,data,{updatedAt:data.updatedAt||new Date().toISOString()});return Promise.resolve(clone(item));},
  deleteWebhook(id,{state}={}){if(state)state.webhooks=(state.webhooks||[]).filter(x=>x.id!==id);return Promise.resolve(true);},
  testWebhook(id,{state}={}){const item=(state?.webhooks||[]).find(x=>x.id===id);if(item)Object.assign(item,{lastDeliveryAt:new Date().toISOString(),lastDeliveryStatus:'success'});return Promise.resolve(clone(item));},
  importEmployees(rows,{state}={}){(state.users||(state.users=[])).push(...rows);return Promise.resolve({created:rows.length,updated:0,ignored:0,errors:[]});},
  exportData(type,filters={}, {state}={}){return Promise.resolve(clone((state?.[type]||[]).filter(x=>!filters.companyId||x.companyId===filters.companyId)));},

  saveChanges({storeKey,state}){this.saveStore({storeKey,state});return clone(state);}
};
})();
