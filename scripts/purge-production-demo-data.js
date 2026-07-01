#!/usr/bin/env node
'use strict';
const args=new Set(process.argv.slice(2));
const dryRun=!args.has('--apply');
if(!args.has('--dry-run')&&!args.has('--apply')){
  console.log('Modo padrão seguro: --dry-run. Use --apply para aplicar.');
}
if(args.has('--dry-run')&&args.has('--apply')){
  console.error('Use apenas um modo: --dry-run ou --apply.');
  process.exit(1);
}
async function main(){
  let admin;
  try{admin=require('firebase-admin');}catch(err){console.error('firebase-admin não disponível. Rode npm install antes de aplicar.');process.exit(1);}
  if(!admin.apps.length)admin.initializeApp({projectId:process.env.GCLOUD_PROJECT||process.env.FIREBASE_PROJECT||'gestordepesquisa'});
  const db=admin.firestore();
  const TS=admin.firestore.FieldValue.serverTimestamp;
  const isDemoRecord=row=>{const id=String(row?.id||row?.surveyId||row?.formId||row?.companyId||row?.organizationId||'').toLowerCase();const slug=String(row?.slug||row?.publicSlug||row?.org||'').toLowerCase();const title=String(row?.title||row?.name||row?.publicName||row?.legalName||'').toLowerCase();const token=String(row?.token||row?.publicToken||row?.accessToken||'').toLowerCase();return row?.isDemo===true||row?.demo===true||row?.fixture===true||row?.sample===true||row?.seeded===true||id.includes('demo')||id.includes('exemplo')||id.includes('example')||slug.includes('empresa-exemplo')||slug.includes('demo')||slug.includes('example')||title.includes('empresa exemplo')||title.includes('demo')||token.includes('demo-token');};
  const forbidden=url=>/survey_demo|empresa-exemplo|demo-token|tokenHash=/i.test(String(url||''));
  const report={dryRun,archivedSurveys:[],archivedForms:[],archivedCompanies:[],archivedInvitations:[],demoResponsesFound:[],removedFeaturedFlags:0,demoUrlsFound:[]};
  const batch=db.batch();let writes=0;
  const add=(ref,patch)=>{if(!dryRun){batch.set(ref,patch,{merge:true});writes++;}};
  for(const name of ['surveys','forms','organizations','companies','invitations','responses']){
    const snap=await db.collection(name).get();
    for(const doc of snap.docs){const row={id:doc.id,...doc.data()};[row.publicUrl,row.publicLink,row.url,row.link,row.shareUrl].filter(forbidden).forEach(url=>report.demoUrlsFound.push({collection:name,id:doc.id,url}));if(!isDemoRecord(row))continue;if(name==='responses'){report.demoResponsesFound.push(doc.id);continue;}if(name==='surveys'){report.archivedSurveys.push(doc.id);if(row.featuredOnHome||row.isFeatured||row.homeFeatured||row.visibleOnHome)report.removedFeaturedFlags++;add(doc.ref,{featuredOnHome:false,isFeatured:false,homeFeatured:false,visibleOnHome:false,homePinned:false,revoked:true,revokedAt:TS(),status:'archived',archived:true,deleted:true,updatedAt:TS()});}else if(name==='forms'){report.archivedForms.push(doc.id);add(doc.ref,{status:'archived',archived:true,deleted:true,updatedAt:TS()});}else if(name==='organizations'||name==='companies'){report.archivedCompanies.push(`${name}/${doc.id}`);add(doc.ref,{status:'archived',archived:true,deleted:true,updatedAt:TS()});}else{report.archivedInvitations.push(doc.id);add(doc.ref,{status:'revoked',revoked:true,deleted:true,updatedAt:TS()});}}
  }
  if(writes)await batch.commit();
  console.log(JSON.stringify(report,null,2));
}
main().catch(err=>{console.error(err);process.exit(1);});
