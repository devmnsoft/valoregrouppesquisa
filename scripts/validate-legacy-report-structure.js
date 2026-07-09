const {read,ok}=require('./legacy-final-validator-lib');const a=read('app.js');
['Resultado geral','Enquadramento geral sem adoçamento','Leitura executiva da realidade','Diagnóstico por dimensão','Radar organizacional','Benchmarking estrutural','Verdade estratégica central','Risco se nada mudar','Próximo nível','Próximo passo natural','Fale com o Valora Group'].forEach(x=>ok(a.includes(x),`relatório sem ${x}`));
ok(!/Valora Pulse™[\s\S]{0,80}Devolutiva estratégica/.test(a),'PDF usa Pulse como devolutiva');
console.log('legacy report structure: PASS');
