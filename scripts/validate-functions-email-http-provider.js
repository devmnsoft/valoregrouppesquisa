const {ok,fn}=require('./_legacy-final-validators');
ok(/EMAIL_PROVIDER/.test(fn)&&/function emailProviderConfig/.test(fn),'email HTTP provider config exists');
ok(/async function sendEmailViaHttpApi/.test(fn)&&/authorization.*Bearer/.test(fn),'HTTP API sender exists');
ok(/provider==='http_api'/.test(fn)&&/sendResultEmailViaSmtp/.test(fn),'HTTP primary with SMTP fallback');
