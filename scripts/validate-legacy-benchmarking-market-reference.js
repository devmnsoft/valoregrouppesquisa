const {read,ok}=require('./legacy-final-validator-lib');const a=read('app.js');
for(const x of ['MARKET_BENCHMARK_REFERENCES','buildStructuralBenchmarking','GPTW Brasil','Esta comparação é qualitativa','não representa pontuação oficial']) ok(a.includes(x),`benchmark sem ${x}`);
if(/certificação GPTW oficial|ranking oficial GPTW/.test(a)) throw new Error('afirmação oficial indevida');
console.log('legacy benchmarking market reference: PASS');
