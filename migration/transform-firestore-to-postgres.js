#!/usr/bin/env node
const fs=require('fs');const path=require('path');const inDir=path.join(__dirname,'export');const outDir=path.join(__dirname,'out');fs.mkdirSync(outDir,{recursive:true});
const read=n=>{const f=path.join(inDir,`${n}.json`);return fs.existsSync(f)?JSON.parse(fs.readFileSync(f,'utf8')):[]};
const write=(n,rows)=>fs.writeFileSync(path.join(outDir,`${n}.json`),JSON.stringify(rows,null,2));
const companies=[...read('companies'),...read('organizations')];
write('organizations',companies.map(x=>({id:x.id,name:x.name||x.companyName||'Organização sem nome',document:x.document||x.cnpj||null,status:x.status||'active'})));
write('users',read('users').map(x=>({id:x.id,organizationId:x.companyId||x.organizationId||null,email:x.email,name:x.name||x.displayName,role:x.role||'user',passwordHash:x.passwordHash||null})));
write('plans',read('plans').map(x=>({id:x.id,code:x.code||x.slug||x.id,name:x.name,price:x.price||0,status:x.status||'active'})));
write('forms',read('forms').map(x=>({id:x.id,organizationId:x.companyId||x.organizationId||null,title:x.title||x.name,status:x.status||'active'})));
write('questions',read('forms').flatMap(f=>(f.questions||[]).map((q,i)=>({id:q.id||`${f.id}_q${i+1}`,formId:f.id,text:q.text||q.title,type:q.type||'scale',weight:q.weight||1}))));
write('surveys',read('surveys').map(x=>({id:x.id,formId:x.formId,organizationId:x.companyId||x.organizationId,status:x.status||'active',publicToken:x.token||x.publicToken||null})));
write('responses',read('responses').map(x=>({id:x.id,surveyId:x.surveyId,participant:x.participant||{},totalScore:x.totalScore||x.score||0,resultToken:x.resultToken||null,createdAt:x.createdAt||null})));
write('answers',read('responses').flatMap(r=>Object.entries(r.answers||{}).map(([questionId,value])=>({responseId:r.id,questionId,value}))));
write('results',read('responses').map(x=>({responseId:x.id,totalScore:x.totalScore||x.score||0,maturityLabel:x.maturityLabel||x.level||null})));
write('communications',read('communications').map(x=>({id:x.id,responseId:x.responseId||null,channel:x.channel||'email',status:x.status||'pending'})));
console.log('transform-firestore-to-postgres: arquivos gerados em migration/out.');
