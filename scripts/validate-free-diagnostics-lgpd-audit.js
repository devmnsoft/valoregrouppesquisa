#!/usr/bin/env node
const {has}=require('./validator-utils');
has('FREE_SURVEY_LGPD_AUDIT.md','free_survey.started','free_survey.submitted','free_survey.result_generated','free_survey.email_job_created','free_survey.email_sent','free_survey.email_failed','free_survey.certificate_generated','free_survey.whatsapp_cta_clicked','metadata sanitizada');
has('backend/Valora.Application/FreeDiagnostics/FreeDiagnosticsAppService.cs','AuditAsync','free_survey.result_email_resent','free_survey.certificate_regenerated');
console.log('validate-free-diagnostics-lgpd-audit: PASS');
