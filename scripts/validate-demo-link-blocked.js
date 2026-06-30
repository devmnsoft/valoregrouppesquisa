const fs=require('fs');
const app=fs.readFileSync('app.js','utf8');
for(const x of ["function isDemoPublicSurveyLink","surveyId === 'survey_demo'","org === 'empresa-exemplo'","resolveProductionPublicSurveyLink","Este link de demonstração não é válido em produção"]){
  if(!app.includes(x))throw new Error('ausente: '+x);
}
console.log('legacy demo link blocked: PASS');
