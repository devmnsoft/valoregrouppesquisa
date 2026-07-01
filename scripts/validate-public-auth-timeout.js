const fs=require('fs');const app=fs.readFileSync('app.js','utf8');const fail=[];
if(!/lastPublicAuthTimeout/.test(app))fail.push('lastPublicAuthTimeout ausente.');
if(!/Auth ainda carregando; seguindo em modo visitante para rota pública/.test(app))fail.push('log público informativo ausente.');
if(/console\.warn\('\[Valora Pulse\] Auth demorou mais de 5s; liberando modo público visitante/.test(app))fail.push('timeout público ainda loga warning antigo.');
if(!/publicNow\?5000:15000/.test(app))fail.push('timeouts público/admin não separados.');
if(!/no visitor release for admin\/private/.test(app))fail.push('proteção explícita de rota privada ausente.');
if(/else[\s\S]{0,400}releasePublicUi\('public_auth_timeout'\)/.test(app))fail.push('rota admin pode liberar visitante.');
if(!/Carregando sessão segura/.test(app)||!/Tentar novamente/.test(app)||!/Ir para login/.test(app))fail.push('UX de timeout privado incompleta.');
if(fail.length){console.error(fail.join('\n'));process.exit(1)}console.log('validate-public-auth-timeout: PASS');
