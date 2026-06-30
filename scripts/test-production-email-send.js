#!/usr/bin/env node
if(process.env.ALLOW_PRODUCTION_EMAIL_SEND!=='true'){console.log('SKIP: defina ALLOW_PRODUCTION_EMAIL_SEND=true para smoke real controlado.');process.exit(0)}console.log('Smoke real deve chamar sendResultEmail com responseId real e idempotencyKey; segredo não é logado.');
