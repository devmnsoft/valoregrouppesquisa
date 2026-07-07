const {ok,fn,app}=require('./_legacy-final-validators');
ok(/failed_non_blocking/.test(fn)&&/errorCode/.test(fn)&&/errorMessage/.test(fn),'functions return non-blocking failure with detail');
ok(/Resultado enviado para o e-mail informado/.test(app)&&/Código:/.test(app)&&/envio por e-mail está em processamento/.test(app),'frontend honest email statuses');
