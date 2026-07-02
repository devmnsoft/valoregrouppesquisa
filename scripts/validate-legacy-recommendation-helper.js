const fs=require('fs');
const app=fs.readFileSync('app.js','utf8');
function fail(m){console.error(m);process.exit(1)}
if(app.includes('recommendationFor(')&&!app.includes('function recommendationFor'))fail('app.js chama recommendationFor mas não declara function recommendationFor.');
for(const s of ['function getRecommendationFor','function resultRecommendationFor','window.recommendationFor = recommendationFor']) if(!app.includes(s)) fail(`Alias/exposição ausente: ${s}`);
console.log('OK legacy recommendation helper');
