'use strict';
const fs=require('fs');
const path=require('path');
function fail(m){console.error(`validate-legacy-plan-catalog: FAIL - ${m}`);process.exit(1);}
function readJson(file){try{return JSON.parse(fs.readFileSync(file,'utf8'));}catch(err){fail(`${file} inválido ou ausente: ${err.message}`);}}
const sharedPath='shared/plan-catalog.json';
const generatedPath='functions/generated/plan-catalog.json';
const catalog=readJson(sharedPath);
const generated=readJson(generatedPath);
for(const code of ['free','essential','growth','professional','corporate','enterprise']){
 if(!catalog[code])fail(`plano ausente: ${code}`);
 for(const k of ['activeSurveys','monthlyResponses','managers','employees','monthlyEmails','clients','companies','units']) if(!(k in catalog[code].limits)) fail(`limite ${k} ausente em ${code}`);
 for(const k of ['executiveReport','internalBenchmark','consolidatedReports','multipleUnits','multipleCompanies','whiteLabel','actionPlan','executiveFollowUp','integrations','exports']) if(!(k in catalog[code].capabilities)) fail(`capacidade ${k} ausente em ${code}`);
}
if(JSON.stringify(catalog)!==JSON.stringify(generated))fail('functions/generated/plan-catalog.json está diferente de shared/plan-catalog.json. Execute node scripts/sync-functions-plan-catalog.js.');
const fn=fs.readFileSync('functions/index.js','utf8');
if(fn.includes("require('../shared/plan-catalog.json')")||fn.includes('require("../shared/plan-catalog.json")'))fail('Functions não pode importar ../shared/plan-catalog.json. Use ./generated/plan-catalog.json.');
if(!fn.includes("require('./generated/plan-catalog.json')")&&!fn.includes('require("./generated/plan-catalog.json")')) fail('Functions deve consumir ./generated/plan-catalog.json.');
const badRequires=[...fn.matchAll(/require\(\s*['"](\.\.\/[^'"]+)['"]\s*\)/g)].map(m=>m[1]);
if(badRequires.length)fail(`require relativo saindo de functions/ proibido: ${badRequires.join(', ')}`);
const firebase=fs.readFileSync('firebase.json','utf8');
if(!/"predeploy"\s*:\s*\[[\s\S]*node scripts\/sync-functions-plan-catalog\.js[\s\S]*\]/.test(firebase))fail('firebase.json deve executar sync-functions-plan-catalog.js no predeploy de functions.');
console.log('validate-legacy-plan-catalog: PASS');
