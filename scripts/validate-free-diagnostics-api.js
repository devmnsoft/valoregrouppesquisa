#!/usr/bin/env node
const {has}=require('./validator-utils');
has('backend/Valora.Api/Controllers/FreeDiagnosticsController.cs','[Authorize]','/admin/free-diagnostics','/summary','resend-email','regenerate-certificate','mark-communication-reviewed','ILogger');
has('backend/Valora.Application/DTOs/FreeDiagnostics/FreeDiagnosticDtos.cs','FreeDiagnosticSummaryDto','FreeDiagnosticListItemDto','FreeDiagnosticDetailDto','ResendResultEmailRequest','RegenerateCertificateRequest','MarkCommunicationReviewedRequest');
has('backend/Valora.Application/FreeDiagnostics/FreeDiagnosticsAppService.cs','admin_valora','consultor_valora','empresa_admin','Tenant');
has('backend/Valora.Infrastructure/Repositories/FreeDiagnosticsRepository.cs','valorapesquisa.responses','valorapesquisa.surveys','valorapesquisa.email_jobs','valorapesquisa.certificates','valorapesquisa.audit_logs','valorapesquisa.organizations','ORDER BY r.created_at DESC');
console.log('validate-free-diagnostics-api: PASS');
