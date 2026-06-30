#!/usr/bin/env node
const fs=require('fs');const path=require('path');
const root=path.resolve(__dirname,'..');let errors=[];
const must=p=>{if(!fs.existsSync(path.join(root,p)))errors.push(`Ausente: ${p}`)};
['ValoraPesquisa.sln','src/ValoraPesquisa.Domain/ValoraPesquisa.Domain.csproj','src/ValoraPesquisa.Application/ValoraPesquisa.Application.csproj','src/ValoraPesquisa.Infrastructure/ValoraPesquisa.Infrastructure.csproj','src/ValoraPesquisa.Api/ValoraPesquisa.Api.csproj','src/ValoraPesquisa.Web/ValoraPesquisa.Web.csproj','database/postgresql/scriptbd_completo.sql','README.md','docker-compose.yml'].forEach(must);
['ApiBase.cs','AuthController.cs','OrganizationsController.cs','UsersController.cs','FormsController.cs','SurveysController.cs','SurveyLinksController.cs','PublicSurveysController.cs','PublicResultsController.cs','ResponsesController.cs','AuditController.cs'].forEach(f=>must(`src/ValoraPesquisa.Api/Controllers/${f}`));
['Scope.cs','AuditService.cs','OrganizationRepository.cs','UserRepository.cs','FormRepository.cs','SurveyRepository.cs','SurveyLinkRepository.cs','ResponseRepository.cs'].forEach(f=>must(`src/ValoraPesquisa.Infrastructure/Repositories/${f}`));
function readDir(dir){let out=''; if(!fs.existsSync(dir))return out; for(const e of fs.readdirSync(dir,{withFileTypes:true})){const p=path.join(dir,e.name); if(e.isDirectory())out+=readDir(p); else if(/\.(cs|cshtml|js|sql)$/.test(e.name))out+=fs.readFileSync(p,'utf8')+'\n';} return out;}
const apiDto=readDir(path.join(root,'src/ValoraPesquisa.Api/Controllers'))+readDir(path.join(root,'src/ValoraPesquisa.Application/DTOs'));
['password_hash','token_hash','result_token_hash','Array.Empty<object>()','return-on-create-only'].forEach(s=>{if(apiDto.includes(s))errors.push(`Texto proibido em API/DTOs: ${s}`)});
const views=readDir(path.join(root,'src/ValoraPesquisa.Web/Views'));
['placeholder','em construção','JSON bruto','StackTrace'].forEach(s=>{if(views.toLowerCase().includes(s.toLowerCase()))errors.push(`Texto proibido nas telas oficiais: ${s}`)});
const sql=fs.readFileSync(path.join(root,'database/postgresql/scriptbd_completo.sql'),'utf8');
['ix_users_email','ix_users_organization_id','ix_organizations_slug','ix_forms_organization_id','ix_surveys_organization_id','ix_survey_links_survey_id','ix_survey_links_token_hash','ix_responses_organization_id','ix_responses_survey_id','ix_responses_result_token_hash','ix_audit_logs_organization_created_at'].forEach(i=>{if(!sql.includes(i))errors.push(`Índice ausente: ${i}`)});
if(errors.length){console.error('Validação backend-v2 falhou:\n- '+errors.join('\n- '));process.exit(1)}
console.log('Validação backend-v2 foundation OK.');
