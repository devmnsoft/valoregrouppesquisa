const fs=require('fs');const app=fs.readFileSync('app.js','utf8');function fail(m){console.error(m);process.exit(1)}
const m=app.match(/function calculateSurveyResult\(f,answers\)\{[\s\S]*?\n\}\nconst calculateResult=calculateSurveyResult;/);if(!m)fail('calculateSurveyResult/calculateResult não localizado.');
const body=m[0];
for(const s of ['payload.level=payload.level||{}','payload.level.title','payload.level.label','payload.level.recommendation','recommendationFor(payload.level,f,payload)']) if(!body.includes(s)) fail(`calculateResult não garante recommendation: ${s}`);
console.log('OK calculateResult recommendation');
