'use strict';
const fs=require('fs');
const catalog=JSON.parse(fs.readFileSync('shared/plan-catalog.json','utf8'));
for(const code of ['free','essential','growth','professional','corporate','enterprise']){
 if(!catalog[code])throw new Error(`plano ausente: ${code}`);
 for(const k of ['activeSurveys','monthlyResponses','managers','employees','monthlyEmails','clients','companies','units']) if(!(k in catalog[code].limits)) throw new Error(`limite ${k} ausente em ${code}`);
 for(const k of ['executiveReport','internalBenchmark','consolidatedReports','multipleUnits','multipleCompanies','whiteLabel','actionPlan','executiveFollowUp','integrations','exports']) if(!(k in catalog[code].capabilities)) throw new Error(`capacidade ${k} ausente em ${code}`);
}
const fn=fs.readFileSync('functions/index.js','utf8');
if(!fn.includes("require('../shared/plan-catalog.json')")) throw new Error('Functions não consome catálogo compartilhado');
console.log('validate-legacy-plan-catalog: PASS');
