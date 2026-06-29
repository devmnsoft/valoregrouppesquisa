const fs=require('fs');
const req={
 'dashboard-page.js':['loadDashboard','renderDashboardCards','renderDashboardHealth','renderDashboardUsage','renderRecentSurveys','renderRecentResponses'],
 'organization-page.js':['loadOrganization','renderOrganizationForm','saveOrganization'],
 'users-page.js':['loadUsers','renderUsersTable','openUserModal','saveUser','toggleUserStatus'],
 'forms-page.js':['loadForms','renderFormsTable','renderFormDetails','saveForm'],
 'surveys-page.js':['loadSurveys','renderSurveysTable','openSurveyModal','saveSurvey','changeSurveyStatus'],
 'public-links-page.js':['loadPublicLinks','renderPublicLinksTable','createPublicLink','copyPublicLink','togglePublicLinkStatus'],
 'responses-page.js':['loadResponses','renderResponsesTable','openResult','openCertificate','sendResultEmail'],
 'communications-page.js':['loadEmailJobs','renderEmailJobs','processEmailQueue'],
 'audit-page.js':['loadAuditEvents','renderAuditEvents'],
 'migration-page.js':['loadMigrationStatus','renderMigrationStatus'],
 'environment-status-page.js':['loadEnvironmentStatus','renderHealthCards'],
 'settings-page.js':['loadSettings','renderSettingsForm','saveSettings'],
 'plans-page.js':['loadPlans','renderPlanCards']
};
let miss=[]; for(const [file,fns] of Object.entries(req)){const p='backend/Valora.Web/wwwroot/js/pages/'+file; if(!fs.existsSync(p)){miss.push(p);continue} const s=fs.readFileSync(p,'utf8'); for(const fn of fns){if(!new RegExp(`\\bfunction\\s+${fn}\\b|\\bconst\\s+${fn}\\s*=|\\b${fn}\\s*[:=]\\s*(?:async\\s*)?function`).test(s))miss.push(`${p}: ${fn}`)}}
if(miss.length){console.error('Renderizadores específicos ausentes:\n'+miss.join('\n'));process.exit(1)}
console.log('validate-aspnet-web-specific-module-renderers: PASS');
