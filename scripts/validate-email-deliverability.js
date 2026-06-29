#!/usr/bin/env node
const {has}=require('./sprint48-validator-lib');
has('EMAIL_DELIVERABILITY_GUIDE.md',['SPF','DKIM','DMARC','reply-to','fila','reenviar']);
has('backend/Valora.Api/Controllers/CommunicationsController.cs',['/admin/email/deliverability/status','fromEmailConfigured','smtpConfigured','spfDocumented','dkimDocumented','dmarcDocumented','canSend']);
console.log('validate-email-deliverability: PASS');
