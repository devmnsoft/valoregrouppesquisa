(function(){
'use strict';
const MODULE_DEFINITIONS=[
{id:'clientes',label:'Clientes',description:'Cadastro comercial e operacional de empresas.',scope:'valora',requiredPermission:'canManageCompanies',defaultEnabled:true,commercialFeature:false,route:'admin/clients'},
{id:'financeiro',label:'Financeiro',description:'Receita, faturas e informações comerciais sensíveis.',scope:'valora',requiredPermission:'canViewFinance',defaultEnabled:true,commercialFeature:false,route:'admin/finance'},
{id:'planos',label:'Planos',description:'Catálogo comercial, limites e módulos liberados.',scope:'valora',requiredPermission:'canManagePlans',defaultEnabled:true,commercialFeature:false,route:'admin/plans'},
{id:'modulos',label:'Módulos',description:'Matriz de módulos e disponibilidade global.',scope:'valora',requiredPermission:'canManageModules',defaultEnabled:true,commercialFeature:false,route:'admin/modules'},
{id:'usuarios',label:'Usuários',description:'Usuários de portal e perfis administrativos.',scope:'empresa',requiredPermission:'canManageCompanyUsers',defaultEnabled:true,commercialFeature:false,route:'admin/users'},
{id:'funcionarios',label:'Funcionários',description:'Funcionários, respondentes, gestores e convidados.',scope:'empresa',requiredPermission:'canManageCompanyUsers',defaultEnabled:true,commercialFeature:true,route:'empresa/users'},
{id:'formularios',label:'Formulários',description:'Questionários, provas, dimensões e pontuação.',scope:'empresa',requiredPermission:'canCreateForms',defaultEnabled:true,commercialFeature:true,route:'empresa/forms'},
{id:'pesquisas',label:'Pesquisas',description:'Campanhas com link seguro, validade e tokens.',scope:'empresa',requiredPermission:'canCreateSurveys',defaultEnabled:true,commercialFeature:true,route:'empresa/surveys'},
{id:'convitesEmail',label:'Convites por e-mail',description:'Envio de convites e lembretes por e-mail.',scope:'empresa',requiredPermission:'canSendInvites',defaultEnabled:false,commercialFeature:true,route:'empresa/users'},
{id:'respostas',label:'Respostas',description:'Consulta de respostas individuais e consolidadas.',scope:'empresa',requiredPermission:'canViewResponses',defaultEnabled:true,commercialFeature:true,route:'empresa/responses'},
{id:'relatorios',label:'Relatórios',description:'Relatórios executivos, gráficos e indicadores.',scope:'empresa',requiredPermission:'canViewReports',defaultEnabled:true,commercialFeature:true,route:'empresa/reports'},
{id:'certificados',label:'Certificados',description:'Certificados individuais de participação/resultado.',scope:'participante',requiredPermission:'canAnswerSurveys',defaultEnabled:true,commercialFeature:true,route:'participante/certificates'},
{id:'actionPlans',label:'Plano de ação',description:'Ações de melhoria, responsáveis, prazos, evidências e acompanhamento.',scope:'empresa',requiredPermission:'canViewReports',defaultEnabled:true,commercialFeature:true,route:'empresa/actionPlans'},
{id:'valorabot',label:'ValoraBot',description:'Assistente contextual de produto e operação.',scope:'empresa',requiredPermission:'canAccessPortal',defaultEnabled:true,commercialFeature:true,route:'bot'},
{id:'lgpd',label:'LGPD',description:'Privacidade, consentimento e direitos do titular.',scope:'publico',requiredPermission:'canAnswerSurveys',defaultEnabled:true,commercialFeature:false,route:'lgpd'},
{id:'exportacoes',label:'Exportações',description:'Exportação de dados e relatórios em múltiplos formatos.',scope:'empresa',requiredPermission:'canViewReports',defaultEnabled:false,commercialFeature:true,route:'empresa/reports'},
{id:'benchmark',label:'Benchmark',description:'Comparativos internos e evolutivos por área/período.',scope:'empresa',requiredPermission:'canViewReports',defaultEnabled:false,commercialFeature:true,route:'empresa/reports'},
{id:'whiteLabel',label:'White label',description:'Personalização avançada de marca.',scope:'empresa',requiredPermission:'canManageCompanyUsers',defaultEnabled:false,commercialFeature:true,route:'empresa/settings'},
{id:'backup',label:'Backup',description:'Exportação/importação completa do ambiente.',scope:'valora',requiredPermission:'canBackup',defaultEnabled:true,commercialFeature:false,route:'admin/backup'},
{id:'logs',label:'Logs',description:'Auditoria global e eventos sensíveis.',scope:'valora',requiredPermission:'canViewLogs',defaultEnabled:true,commercialFeature:false,route:'admin/logs'}
];
function getModuleDefinition(moduleId){return MODULE_DEFINITIONS.find(m=>m.id===moduleId)||null;}
function moduleEnabledGlobally(moduleId){const state=window.ValoraState;const found=state?.modules?.find(m=>m.id===moduleId);return found?found.enabled!==false:(getModuleDefinition(moduleId)?.defaultEnabled!==false);}
function moduleEnabledForPlan(plan,moduleId){if(!plan)return false;const enabled=Array.isArray(plan.enabledModules)?plan.enabledModules:[];return enabled.includes(moduleId)||(!enabled.length&&getModuleDefinition(moduleId)?.defaultEnabled===true);}
function moduleEnabledForCompany(company,moduleId){const plans=(window.ValoraState&&window.ValoraState.plans)||[];const plan=plans.find(p=>p.id===(company?.planId||company?.plan));return moduleEnabledGlobally(moduleId)&&moduleEnabledForPlan(plan,moduleId);}
function canUseModule(user,company,moduleId){const mod=getModuleDefinition(moduleId),roles=window.ValoraRoles;if(!user||!mod||!roles)return false;if(!moduleEnabledGlobally(moduleId))return false;if(!roles.can(user,mod.requiredPermission))return false;if(user.role==='admin_valora')return true;if(mod.scope==='valora')return ['admin_valora','consultor_valora'].includes(user.role);return roles.canAccessCompany(user,company?.id||user.companyId)&&moduleEnabledForCompany(company,moduleId);}
window.ValoraModules={MODULE_DEFINITIONS,getModuleDefinition,moduleEnabledGlobally,moduleEnabledForPlan,moduleEnabledForCompany,canUseModule};
})();
