const fs=require('fs');const s=fs.readFileSync('app.js','utf8');
function fail(m){throw new Error(m)}
if(!/function\s+dimensionRecommendation\s*\(/.test(s))fail('function dimensionRecommendation ausente');
if(!/function\s+getDimensionRecommendation\s*\(/.test(s))fail('alias getDimensionRecommendation ausente');
if(!/window\.dimensionRecommendation\s*=\s*dimensionRecommendation/.test(s))fail('dimensionRecommendation não exposto no window');
const start=s.indexOf('function getCertificateScore'); const end=s.indexOf('function sanitizeCertificateText', start); const score=start>=0?s.slice(start,end):'';
if(!score.includes('dimensionRecommendation('))fail('getCertificateScore não usa dimensionRecommendation');
console.log('legacy dimension recommendation helper: PASS');
