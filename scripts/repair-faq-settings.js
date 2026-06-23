#!/usr/bin/env node
const fs=require('fs');const path=require('path');const {normalizeFaqItems,defaultFaq}=require('../data-normalization');
function args(argv){const o={};for(let i=0;i<argv.length;i++){const a=argv[i];if(a.startsWith('--')){const k=a.slice(2);if(['backup','dry-run','apply'].includes(k))o[k]=true;else o[k]=argv[++i];}}return o;}
const opts=args(process.argv.slice(2));
const metaKeys=['id','updatedBy','updatedAt','migratedAt','source','createdAt','version','storeKey','metadata'];
function typeOf(v){return Array.isArray(v)?'array':(v===null?'null':typeof v)}
async function main(){
 if(opts.apply&&opts['dry-run'])throw new Error('Use --dry-run ou --apply, não ambos.');
 let admin,db;try{admin=require('firebase-admin');if(!admin.apps.length)admin.initializeApp({projectId:opts.project});db=admin.firestore();}catch(e){throw new Error(`Firebase Admin indisponível: ${e.message}`)}
 const snap=await db.collection('settings').get();const doc=snap.docs.find(d=>d.id==='public')||snap.docs[0];
 const out={settingsFound:!!doc,faqBeforeType:'missing',faqAfterType:'array',faqItems:0,removedMetaKeys:[],backup:''};
 if(!doc){console.log(JSON.stringify(out,null,2));return;}
 const data=doc.data()||{},before=data.faq;out.faqBeforeType=typeOf(before);if(before&&typeof before==='object'&&!Array.isArray(before))out.removedMetaKeys=metaKeys.filter(k=>Object.prototype.hasOwnProperty.call(before,k));
 const faq=normalizeFaqItems(before,defaultFaq());out.faqItems=faq.length;
 if(opts.backup){const dir=path.join(process.cwd(),'backups');fs.mkdirSync(dir,{recursive:true});const file=path.join(dir,`settings-faq-${new Date().toISOString().replace(/[:.]/g,'-')}.json`);fs.writeFileSync(file,JSON.stringify({id:doc.id,...data},null,2));out.backup=path.relative(process.cwd(),file).replace(/\\/g,'/');}
 if(opts.apply)await doc.ref.set({...data,faq,updatedAt:new Date().toISOString(),source:data.source||'repair-faq-settings'},{merge:true});
 console.log(JSON.stringify(out,null,2));
}
main().catch(e=>{console.error(e.message);process.exit(1);});
