'use strict';
const fs=require('fs');
const path=require('path');
const root=path.resolve(__dirname,'..');
const source=path.join(root,'shared','plan-catalog.json');
const targetDir=path.join(root,'functions','generated');
const target=path.join(targetDir,'plan-catalog.json');
function fail(message){console.error(`sync-functions-plan-catalog: FAIL - ${message}`);process.exit(1);}
if(!fs.existsSync(source))fail('shared/plan-catalog.json não encontrado.');
let parsed;
try{parsed=JSON.parse(fs.readFileSync(source,'utf8'));}catch(err){fail(`shared/plan-catalog.json inválido: ${err.message}`);}
fs.mkdirSync(targetDir,{recursive:true});
fs.writeFileSync(target,`${JSON.stringify(parsed,null,2)}\n`);
console.log(`sync-functions-plan-catalog: PASS - catálogo sincronizado em ${path.relative(root,target)}`);
