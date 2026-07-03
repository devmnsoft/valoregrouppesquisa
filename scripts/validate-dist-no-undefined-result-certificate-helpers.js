const fs=require('fs'),path=require('path');
const dirs=['dist','public','build'].filter(d=>fs.existsSync(d));
let fail=[];
for(const dir of dirs){
  for(const f of fs.readdirSync(dir).filter(x=>/^app.*\.js$/.test(x))){
    const s=fs.readFileSync(path.join(dir,f),'utf8');
    for(const msg of ['company is not defined','dimensionRecommendation is not defined','recommendationFor is not defined']) if(s.includes(msg)) fail.push(`${f} contém ${msg}`);
    if(s.includes('dimensionRecommendation(')&&!s.includes('function dimensionRecommendation')) fail.push(`${f} chama dimensionRecommendation sem função`);
    if(s.includes('normalizePublicResultViewModel(')&&!s.includes('function normalizePublicResultViewModel')) fail.push(`${f} chama normalizePublicResultViewModel sem função`);
  }
}
if(fail.length){console.error(fail.join('\n'));process.exit(1)}
console.log('dist no undefined result certificate helpers: PASS');
