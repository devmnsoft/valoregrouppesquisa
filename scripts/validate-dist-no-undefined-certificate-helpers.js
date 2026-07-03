const fs=require('fs'),path=require('path');function fail(m){throw new Error(m)}
const dir='dist/assets';if(!fs.existsSync(dir))fail('dist/assets ausente; execute build:prod antes deste validador');
const files=fs.readdirSync(dir).filter(f=>/^app.*\.js$/.test(f));if(!files.length)fail('dist app*.js ausente');
for(const f of files){const s=fs.readFileSync(path.join(dir,f),'utf8');if(s.includes('dimensionRecommendation(')&&!/function\s+dimensionRecommendation\s*\(/.test(s))fail(f+' chama dimensionRecommendation sem função');const idx=s.indexOf('async function renderResult');const end=s.indexOf('function getCertificateScore',idx);const r=idx>=0?s.slice(idx,end>idx?end:idx+8000):'';if(r.includes('certificateHtml(')&&!r.includes('safeCertificateHtml('))fail(f+' renderResult chama certificateHtml sem safe');}
console.log('dist no undefined certificate helpers: PASS');
