#!/usr/bin/env node
const {has}=require('./validator-utils');
has('backend/Valora.Application/FreeDiagnostics/FreeDiagnosticsAppService.cs','CountManualResendsLast24hAsync','>= 3','admin_valora','free_survey.result_email_resent');
has('backend/Valora.Infrastructure/Repositories/FreeDiagnosticsRepository.cs','result-ready-resend','last_resend_at','resend_count');
has('database/postgresql/047_free_diagnostics_email_operations.sql','resend_count','last_resend_at','reviewed_at','reviewed_by','review_note');
console.log('validate-free-diagnostics-email-resend: PASS');
