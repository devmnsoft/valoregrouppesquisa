#!/usr/bin/env node
const fs=require('fs'); const files=['backend/Valora.Web/Views/FreeDiagnostics/Index.cshtml','backend/Valora.Web/wwwroot/js/pages/free-diagnostics-page.js','backend/Valora.Infrastructure/Repositories/FreeDiagnosticsRepository.cs'].map(f=>fs.readFileSync(f,'utf8')).join('\n');
['password_hash','smtp_password','private_key','result_token_hash','token_hash'].forEach(x=>{if(files.includes(x)) throw new Error('dado sensível exposto: '+x)});
if(!files.includes('regexp_replace')) throw new Error('mascara de email ausente');
console.log('validate-free-diagnostics-sensitive-data: PASS');
