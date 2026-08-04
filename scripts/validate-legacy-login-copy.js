const {read,ok}=require('./legacy-final-validator-lib');const a=read('app.js');
ok(a.includes('Entrar no Valora Insight™'),'título login ausente');ok(a.includes('Acesse a gestão do Valora Insight™.'),'subtítulo login ausente');
console.log('legacy login copy: PASS');
