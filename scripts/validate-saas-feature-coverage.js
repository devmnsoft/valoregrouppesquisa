const {assert,readIf,allFiles,pass}=require('./production-gate-utils');
const cs=allFiles(['backend'],['.cs']); const sql=allFiles(['database/postgresql'],['.sql']); const js=['app.js','api-client.js','api-repository.js','repository.js','runtime-capabilities.js','gateway-client.js'].map(readIf).join('\n');
const body=cs.map(readIf).join('\n'); const schema=sql.map(readIf).join('\n');
const modules={
 'Organizações':['organizations','OrganizationRepository','OrganizationsController','organizations'], 'Usuários':['users','UserRepository','AuthController','users'], 'Perfis':['users','UserRepository','AuthController','role'], 'Planos':['plans','PlanRepository','PlansController','plans'], 'Assinaturas':['subscriptions','PlanRepository','OrganizationsController','plan'], 'Formulários':['forms','FormRepository','PublicSurveysController','forms'], 'Dimensões':['form_dimensions','FormRepository','PublicSurveysController','dimensions'], 'Perguntas':['questions','FormRepository','PublicSurveysController','questions'], 'Pesquisas':['surveys','SurveyRepository','PublicSurveysController','surveys'], 'Links públicos':['survey_links','SurveyRepository','PublicSurveysController','public'], 'Respostas':['responses','ResponseRepository','PublicSurveysController','responses'], 'Resultados':['result_scores','ResultRepository','PublicResultsController','result'], 'Certificados':['certificates','CertificateRepository','CertificatesController','certificates'], 'Comunicação':['communications|email_jobs','CommunicationRepository','CommunicationsController','communications'], 'Auditoria':['audit_logs','AuditRepository','HealthController','logs'], 'Migração':['schema_migrations','MigrationRepository','MigrationController','migration'], 'Status do ambiente':['schema_migrations','HealthController','HealthController','status'], 'Configurações da empresa':['organizations','OrganizationRepository','OrganizationsController','settings']
};
const errors=[];
for(const [name,[table,repo,controller,front]] of Object.entries(modules)){
 const t=new RegExp(`valorapesquisa\\.(${table})`, 'i'); if(!t.test(schema)) errors.push(`${name}: migration/tabela ausente`);
 if(!new RegExp(repo,'i').test(body)) errors.push(`${name}: repository ausente`);
 if(!new RegExp(controller,'i').test(body)) errors.push(`${name}: endpoint/controller ausente`);
 if(!new RegExp(front,'i').test(js)) errors.push(`${name}: frontend/app integration ausente`);
 if(!/ILogger<|LogInformation|LogError/.test(body)) errors.push(`${name}: logger ausente`);
 if(!/try\s*\{/.test(body)) errors.push(`${name}: try/catch ausente`);
}
assert(!errors.length, errors.join('\n'));
pass('validate-saas-feature-coverage');
