#!/usr/bin/env node
const {spawnSync}=require('child_process');
const checks=[
  ['app:syntax','npm',['run','app:syntax']],
  ['actions:handlers','npm',['run','actions:handlers']],
  ['public:boot-no-reference','npm',['run','public:boot-no-reference']],
  ['home:featured-consistency','npm',['run','home:featured-consistency']],
  ['data:soft-delete-filtering','npm',['run','data:soft-delete-filtering']],
  ['routes:public-params','npm',['run','routes:public-params']],
  ['public:submit-firebase-only','npm',['run','public:submit-firebase-only']],
  ['firestore:no-undefined','npm',['run','firestore:no-undefined']],
  ['audit:no-save-loop','npm',['run','audit:no-save-loop']],
  ['lgpd:text','npm',['run','lgpd:text']],
  ['public:survey-url-contract','npm',['run','public:survey-url-contract']]
];
const failures=[];
for(const [name,cmd,args] of checks){
  const r=spawnSync(cmd,args,{stdio:'inherit',shell:false});
  if(r.status!==0)failures.push(name);
}
if(failures.length){
  console.error(`check:critical falhou: ${failures.join(', ')}`);
  process.exit(1);
}
console.log('check:critical: PASS');
