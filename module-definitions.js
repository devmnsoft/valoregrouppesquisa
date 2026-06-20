(function(){
'use strict';
const MODULE_DEFINITIONS=[
{id:'clientes',label:'Clientes',description:'Cadastro comercial e operacional de organizações.',scope:'valora',minimumRole:'admin_valora',minimumPlan:null,route:'admin/clients',status:'active'},
{id:'usuarios',label:'Usuários',description:'Usuários de portal e perfis administrativos.',scope:'empresa',minimumRole:'empresa_admin',minimumPlan:null,route:'users',status:'active'},
{id:'funcionarios',label:'Funcionários',description:'Participantes, gestores de área e convidados externos.',scope:'empresa',minimumRole:'empresa_admin',minimumPlan:null,route:'empresa/users',status:'active'},
{id:'formularios',label:'Formulários',description:'Questionários, dimensões, pesos e faixas de resultado.',scope:'empresa',minimumRole:'gestor_pesquisa',minimumPlan:'free',route:'forms',status:'active'},
{id:'pesquisas',label:'Pesquisas',description:'Campanhas, links seguros, status e validade.',scope:'empresa',minimumRole:'gestor_pesquisa',minimumPlan:'free',route:'surveys',status:'active'},
{id:'convitesEmail',label:'Convites por e-mail',description:'Fila de convites e ciclo pending/sent/opened/answered.',scope:'empresa',minimumRole:'gestor_pesquisa',minimumPlan:'essential',route:'surveys',status:'active'},
{id:'respostas',label:'Respostas',description:'Base de respostas concluídas e auditoria de origem.',scope:'empresa',minimumRole:'analista_resultados',minimumPlan:'free',route:'responses',status:'active'},
{id:'relatorios',label:'Relatórios',description:'Dashboards, exportações e relatórios executivos.',scope:'empresa',minimumRole:'analista_resultados',minimumPlan:'free',route:'reports',status:'active'},
{id:'certificados',label:'Certificados',description:'Certificados individuais calculados pela mesma fonte do relatório.',scope:'participante',minimumRole:'participante',minimumPlan:'free',route:'certificates',status:'active'},
{id:'financeiro',label:'Financeiro',description:'Planos, faturas, trial, inadimplência e suspensão.',scope:'valora',minimumRole:'admin_valora',minimumPlan:null,route:'finance',status:'active'},
{id:'planos',label:'Planos',description:'Catálogo comercial, limites e módulos liberados.',scope:'valora',minimumRole:'admin_valora',minimumPlan:null,route:'plans',status:'active'},
{id:'modulos',label:'Módulos',description:'Catálogo de módulos e disponibilidade global.',scope:'valora',minimumRole:'admin_valora',minimumPlan:null,route:'modules',status:'active'},
{id:'valorabot',label:'ValoraBot',description:'Assistente contextual.',scope:'empresa',minimumRole:'participante',minimumPlan:'growth',route:'bot',status:'active'},
{id:'lgpd',label:'LGPD',description:'Consentimento, privacidade e direitos do titular.',scope:'público',minimumRole:'participante',minimumPlan:'free',route:'privacy',status:'active'},
{id:'exportacoes',label:'Exportações',description:'CSV, JSON, PDF, Word e Excel.',scope:'empresa',minimumRole:'analista_resultados',minimumPlan:'essential',route:'reports',status:'active'},
{id:'benchmark',label:'Benchmark',description:'Comparativos internos por dimensão, período e área.',scope:'empresa',minimumRole:'analista_resultados',minimumPlan:'growth',route:'reports',status:'active'},
{id:'whiteLabel',label:'White label',description:'Personalização avançada de marca.',scope:'empresa',minimumRole:'empresa_admin',minimumPlan:'enterprise',route:'settings',status:'planned'},
{id:'backup',label:'Backup',description:'Exportação completa e governança de retenção.',scope:'valora',minimumRole:'admin_valora',minimumPlan:null,route:'backup',status:'active'},
{id:'logs',label:'Logs',description:'Auditoria imutável de eventos sensíveis.',scope:'valora',minimumRole:'admin_valora',minimumPlan:null,route:'logs',status:'active'}
];
function moduleEnabledForPlan(plan,moduleId){if(!plan)return false;const enabled=Array.isArray(plan.enabledModules)?plan.enabledModules:[];return enabled.includes(moduleId)||enabled.includes(moduleId.replace('convitesEmail','pesquisas'));}
function moduleEnabledForCompany(company,moduleId){const plans=(window.ValoraState&&window.ValoraState.plans)||[];const plan=plans.find(p=>p.id===company?.planId);return moduleEnabledForPlan(plan,moduleId);}
function canUseModule(user,company,moduleId){const roles=window.ValoraRoles;if(!user||!roles)return false;const mod=MODULE_DEFINITIONS.find(m=>m.id===moduleId);if(!mod||mod.status==='inactive')return false;if(!roles.canAccessScope(user,company?.id||user.companyId))return false;if(user.role==='admin_valora')return true;return moduleEnabledForCompany(company,moduleId);}
window.ValoraModules={MODULE_DEFINITIONS,moduleEnabledForCompany,moduleEnabledForPlan,canUseModule};
})();
