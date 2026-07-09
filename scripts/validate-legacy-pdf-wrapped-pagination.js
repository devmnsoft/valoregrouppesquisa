const {read,ok}=require('./legacy-final-validator-lib');const p=read('pdf.js');
for(const x of ['ensurePdfPageSpace','writePdfWrappedText','writePdfSection','splitWords']) ok(p.includes(x),`paginação/wrap ausente ${x}`);
console.log('legacy pdf wrapped pagination: PASS');
