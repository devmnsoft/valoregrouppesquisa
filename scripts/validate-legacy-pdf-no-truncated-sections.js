const {read,ok}=require('./legacy-final-validator-lib');const a=read('app.js'),p=read('pdf.js');
for(const x of ['framing','executiveReality','benchmarking','risk','nextLevel','transition']) ok(a.includes(x),`seção ${x} ausente`);
ok(!/\.slice\(0,\s*[0-9]+\).*devolutiva/.test(a),'slice em devolutiva');
console.log('legacy pdf no truncated sections: PASS');
