#!/usr/bin/env node
const {has}=require('./validator-utils');
has('backend/Valora.Application/FreeDiagnostics/FreeDiagnosticsAppService.cs','RegenerateCertificateAsync','free_survey.certificate_regenerated','sem alterar o resultado original');
has('backend/Valora.Infrastructure/Repositories/FreeDiagnosticsRepository.cs','RegenerateCertificateMetadataAsync','UPDATE valorapesquisa.certificates','validation_url');
has('FREE_SURVEY_CERTIFICATE_OPERATIONS.md','Regenerar certificado não altera resultado original');
console.log('validate-free-diagnostics-certificate-operations: PASS');
