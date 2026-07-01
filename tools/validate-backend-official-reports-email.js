const fs=require('fs'), path=require('path');
const root=process.cwd(); const read=p=>fs.existsSync(p)?fs.readFileSync(p,'utf8'):''; let failed=false;
function ok(cond,msg){ if(cond) console.log('OK',msg); else { console.error('FAIL',msg); failed=true; }}
const sql=read('database/postgresql/scriptbd_completo.sql')+read('scriptbd_completo.sql');
['report_definitions','generated_reports','certificates','certificate_validations','export_jobs','lgpd_consents','privacy_requests','email_templates','email_jobs','modules','subscriptions','usage_monthly'].forEach(t=>ok(sql.includes(t),`SQL contém ${t}`));
['ReportsController','OperationalCertificatesController','PublicCertificatesController','ExportsController','LgpdController','EmailController'].forEach(c=>ok(read(`backend/Valora.Api/Controllers/${c}.cs`).includes(c),`controller ${c}`));
['ReportService','CertificateOperationalService','ExportService','LgpdConsentService','PrivacyRequestService','EmailQueueService','MenuService','EntitlementService'].forEach(s=>ok(read('backend/Valora.Application/Services/OperationalServices.cs').includes(s),`service ${s}`));
['ModuleRepository','SubscriptionRepository','UsageRepository','ReportRepository','CertificateOperationalRepository','ExportRepository','LgpdRepository','EmailOperationalRepository'].forEach(r=>ok((read('backend/Valora.Infrastructure/Repositories/SaasRepositories.cs')+read('backend/Valora.Infrastructure/Repositories/OperationalRepositories.cs')).includes(r),`repository ${r}`));
['Reports/Index.cshtml','Exports/Index.cshtml','Lgpd/Index.cshtml','Lgpd/Requests.cshtml','Email/Templates.cshtml','Email/Jobs.cshtml','Email/Status.cshtml'].forEach(v=>ok(fs.existsSync(`backend/Valora.Web/Views/${v}`),`view ${v}`));
const dto=read('backend/Valora.Application/DTOs/OperationalDtos.cs'); ok(!/(password_hash|token_hash|result_token_hash|refresh token|smtp_password|connection string)/i.test(dto),'DTOs operacionais não expõem campos sensíveis');
const webFiles=[]; function walk(d){ if(!fs.existsSync(d)) return; for(const f of fs.readdirSync(d)){ const p=path.join(d,f); const st=fs.statSync(p); if(st.isDirectory()) walk(p); else webFiles.push(p); }} walk('backend/Valora.Web'); ok(!webFiles.some(f=>/VALORA_SMTP_PASSWORD|SMTP_PASSWORD|password_hash|token_hash|result_token_hash/i.test(read(f))),'frontend não expõe senha SMTP/hash/token');
ok(read('package.json').includes('backend:reports-email-validate'),'package.json contém script backend:reports-email-validate');
process.exit(failed?1:0);
