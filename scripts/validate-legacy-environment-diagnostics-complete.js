const {assert,has,done}=require('./_legacy-validator-lib');
['Status do Ambiente','Diagnóstico do Ambiente','Provider de submissão','Provider de resultado','Transporte de e-mail','Último erro de submissão','Último erro de e-mail','SMTP_PASSWORD','Possui token'].forEach(x=>assert(has('app.js',x),`diagnostics missing ${x}`));done('legacy diagnostics');
