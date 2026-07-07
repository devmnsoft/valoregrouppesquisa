const fs=require('fs');const s=fs.readFileSync('functions/index.js','utf8');const fail=[];
for(const k of ['EMAIL_PROVIDER','EMAIL_API_KEY','EMAIL_API_URL','EMAIL_FROM_EMAIL','EMAIL_FROM_NAME','function emailProviderConfig','async function sendEmailViaHttpApi','Bearer ${cfg.apiKey}','provider:\'http_api\'','hasSmtpConfig','sendResultEmailViaSmtp']) if(!s.includes(k)) fail.push('ausente: '+k);
if(!/provider:secretValue\(EMAIL_PROVIDER,'EMAIL_PROVIDER','http_api'\)/.test(s)) fail.push('provider padrão deve ser http_api');
if(!/if\(provider==='http_api'\)[\s\S]*sendEmailViaHttpApi[\s\S]*hasSmtpConfig\(\)[\s\S]*sendResultEmailViaSmtp/.test(s)) fail.push('sendResultEmailInternal deve tentar HTTP API e SMTP fallback');
if(fail.length){console.error(fail.join('\n'));process.exit(1)}console.log('functions email http provider: PASS');
