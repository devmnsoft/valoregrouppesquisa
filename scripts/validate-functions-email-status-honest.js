const fs=require('fs');const fn=fs.readFileSync('functions/index.js','utf8'),app=fs.readFileSync('app.js','utf8');const fail=[];
for(const k of ['failed_non_blocking','errorCode:classified.code','errorMessage:classified.message','status:sent.status','queued:false']) if(!fn.includes(k)) fail.push('functions status honesto ausente: '+k);
for(const k of ['Resultado enviado para o e-mail informado.','Seu resultado foi registrado, mas o e-mail não foi enviado agora.','Código: ${errorCode}','Detalhe: ${errorMessage}','Seu resultado foi registrado e o envio por e-mail está em processamento.']) if(!app.includes(k)) fail.push('mensagem honesta ausente: '+k);
if(/failed_non_blocking[\s\S]{0,180}Resultado enviado para o e-mail informado/.test(app)) fail.push('falha não pode dizer enviado');
if(fail.length){console.error(fail.join('\n'));process.exit(1)}console.log('functions email status honest: PASS');
