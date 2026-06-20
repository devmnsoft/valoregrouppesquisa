(function(){
'use strict';
const ALL_TRUE={canAccessPortal:true,canManageCompanies:true,canManagePlans:true,canManageModules:true,canManageGlobalSettings:true,canManageCompanyUsers:true,canCreateForms:true,canCreateSurveys:true,canSendInvites:true,canViewResponses:true,canViewReports:true,canViewFinance:true,canViewLogs:true,canBackup:true,canAnswerSurveys:true,restrictedToDepartment:false};
const BASE={canAccessPortal:false,canManageCompanies:false,canManagePlans:false,canManageModules:false,canManageGlobalSettings:false,canManageCompanyUsers:false,canCreateForms:false,canCreateSurveys:false,canSendInvites:false,canViewResponses:false,canViewReports:false,canViewFinance:false,canViewLogs:false,canBackup:false,canAnswerSurveys:false,restrictedToDepartment:false};
const ROLE_DEFINITIONS={
 admin_valora:{...BASE,...ALL_TRUE,id:'admin_valora',label:'Administrador Geral Valora',scope:'valora',description:'Administra toda a plataforma Valora Pulse: clientes, planos, módulos, usuários, financeiro, configurações, backup, logs e dados operacionais.'},
 consultor_valora:{...BASE,id:'consultor_valora',label:'Consultor Valora',scope:'valora',description:'Visualiza a operação Valora e acompanha clientes, formulários, pesquisas, respostas e relatórios sem alterar financeiro, planos, backup ou configurações críticas.',canAccessPortal:true,canManageCompanies:true,canViewResponses:true,canViewReports:true},
 empresa_admin:{...BASE,id:'empresa_admin',label:'Administrador da Empresa',scope:'empresa',description:'Administra usuários, funcionários, formulários, pesquisas, convites, respostas, relatórios, plano contratado e dados da própria empresa.',canAccessPortal:true,canManageCompanyUsers:true,canCreateForms:true,canCreateSurveys:true,canSendInvites:true,canViewResponses:true,canViewReports:true,canViewFinance:true,canAnswerSurveys:true},
 gestor_pesquisa:{...BASE,id:'gestor_pesquisa',label:'Gestor de Pesquisa',scope:'empresa',description:'Cria questionários e pesquisas, envia convites e acompanha respostas e relatórios da própria empresa.',canAccessPortal:true,canCreateForms:true,canCreateSurveys:true,canSendInvites:true,canViewResponses:true,canViewReports:true,canAnswerSurveys:true},
 analista_resultados:{...BASE,id:'analista_resultados',label:'Analista de Resultados',scope:'empresa',description:'Visualiza respostas, gráficos, indicadores e relatórios, sem criar formulários, pesquisas ou envios.',canAccessPortal:true,canViewResponses:true,canViewReports:true,canAnswerSurveys:true},
 gestor_area:{...BASE,id:'gestor_area',label:'Gestor de Área',scope:'empresa',description:'Visualiza respostas e relatórios apenas do seu departamento/área.',canAccessPortal:true,canViewResponses:true,canViewReports:true,canAnswerSurveys:true,restrictedToDepartment:true,requiresDepartment:true},
 participante:{...BASE,id:'participante',label:'Participante',scope:'participante',description:'Responde pesquisas e consulta histórico, certificados e privacidade quando permitido.',canAccessPortal:true,canAnswerSurveys:true},
 convidado_externo:{...BASE,id:'convidado_externo',label:'Convidado Externo',scope:'externo',description:'Responde por link seguro e não acessa o portal administrativo.',canAnswerSurveys:true,defaultPortalAccess:false}
};
const GLOBAL_ROLES=['admin_valora','consultor_valora'];
const COMPANY_ROLES=['empresa_admin','gestor_pesquisa','analista_resultados','gestor_area','participante','convidado_externo'];
function getRoleDefinition(role){return ROLE_DEFINITIONS[role]||{...BASE,id:role||'',label:role||'Sem perfil',scope:'unknown',description:'Perfil não reconhecido.'};}
function can(user,permission){if(!user)return false;if(user.role==='admin_valora')return true;return !!getRoleDefinition(user.role)[permission];}
function canAccessCompany(user,companyId){if(!user)return false;if(GLOBAL_ROLES.includes(user.role))return true;return !!companyId&&user.companyId===companyId;}
function canAccessScope(user,companyId){return canAccessCompany(user,companyId);}
function canCreateRole(currentUser,targetRole){if(!currentUser||!ROLE_DEFINITIONS[targetRole])return false;if(currentUser.role==='admin_valora')return true;if(currentUser.role==='empresa_admin')return COMPANY_ROLES.includes(targetRole);return false;}
function availableRolesForCurrentUser(currentUser){return Object.keys(ROLE_DEFINITIONS).filter(role=>canCreateRole(currentUser,role));}
function availableCompanyRolesFor(currentUser){return availableRolesForCurrentUser(currentUser).filter(role=>COMPANY_ROLES.includes(role));}
window.ValoraRoles={ROLE_DEFINITIONS,GLOBAL_ROLES,COMPANY_ROLES,getRoleDefinition,can,canAccessCompany,canAccessScope,canCreateRole,availableRolesForCurrentUser,availableCompanyRolesFor,availableRolesForUser:availableRolesForCurrentUser,availableRolesForCompany:availableCompanyRolesFor};
})();
