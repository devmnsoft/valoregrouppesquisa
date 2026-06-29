#!/usr/bin/env node
const {has}=require('./validator-utils');
has('backend/Valora.Web/Controllers/FreeDiagnosticsController.cs','FreeDiagnosticsController','Authorize');
has('backend/Valora.Web/Views/FreeDiagnostics/Index.cshtml','Diagnósticos gratuitos','data-filters','Reenviar','Falar com especialista Valora','canViewResponses');
has('backend/Valora.Web/wwwroot/js/pages/free-diagnostics-page.js','FreeDiagnosticsApi.summary','data-resend','data-whatsapp','correlationId');
has('backend/Valora.Web/wwwroot/js/api/free-diagnostics-api.js','/admin/free-diagnostics','resend-email','regenerate-certificate','mark-communication-reviewed');
console.log('validate-free-diagnostics-admin-panel: PASS');
