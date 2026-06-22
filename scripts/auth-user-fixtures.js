'use strict';
const TEST_AUTH_USERS=[
  { email:'admin@valoragroup.com', password:'Valora@2026', role:'admin_valora', companyId:'' },
  { email:'consultor@valoragroup.com', password:'Valora@2026', role:'consultor_valora', companyId:'' },
  { email:'gestor@empresa.com', password:'Empresa@2026', role:'empresa_admin', companyId:'org_demo' },
  { email:'rh@empresa.com', password:'Empresa@2026', role:'gestor_pesquisa', companyId:'org_demo' },
  { email:'participante@empresa.com', password:'123456', role:'participante', companyId:'org_demo' }
];
const DIAGNOSE_EMAILS=[...TEST_AUTH_USERS.map(u=>u.email),'marcelonsva@gmail.com'];
function publicUserDoc(rec,fixture={}){return {uid:rec.uid,legacyId:fixture.legacyId||'',name:rec.displayName||fixture.name||fixture.email||rec.email||'',email:rec.email||fixture.email,role:fixture.role||rec.customClaims?.role||'participante',companyId:fixture.companyId??rec.customClaims?.companyId??'',status:rec.disabled?'inactive':'active',portalAccess:true,receivesEmail:true,updatedAt:new Date().toISOString(),source:'auth_maintenance'};}
module.exports={TEST_AUTH_USERS,DIAGNOSE_EMAILS,publicUserDoc};
