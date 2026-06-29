#!/usr/bin/env node
const {has}=require('./sprint48-validator-lib');
has('backend/Valora.Web/Controllers/OperationsController.cs',['OperationsController']);
has('backend/Valora.Web/Views/Operations/Index.cshtml',['status da API','status do banco','status do SMTP','status da fila de e-mail','status do certificado público','Firebase legado','Processar fila de e-mail']);
has('backend/Valora.Web/wwwroot/js/pages/operations-page.js',['password','connection string','resultToken','stack trace']);
console.log('validate-operations-panel: PASS');
