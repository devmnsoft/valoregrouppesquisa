const {assert,has,done}=require('./_legacy-validator-lib');
['sendResultEmailAuto','sendResultEmailViaFunction','sendResultEmailViaApi','Resultado gerado com sucesso. Enviamos uma cópia','não conseguimos enviar o e-mail','responseId real obrigatório','resultToken real obrigatório'].forEach(x=>assert(has('app.js',x),`app missing ${x}`));
['SMTP_PASSWORD','valoragroup@mnsoft.com.br','emailJobs','pending_retry','resultTokenHash'].forEach(x=>assert(has('functions/index.js',x),`functions missing ${x}`));done('legacy result email');
