#!/usr/bin/env node
const {has}=require('./sprint48-validator-lib');
has('backend/Valora.Api/Controllers/OperationsController.cs',['[Authorize]','/admin/operations/health','/admin/operations/email','/admin/operations/free-survey','/admin/operations/certificates','/admin/operations/email/process-queue','/admin/operations/free-survey/repair-link','logger']);
console.log('validate-operations-api: PASS');
