#!/usr/bin/env node
'use strict';
const fs=require('fs');
const argv=process.argv.slice(2);
const has=f=>argv.includes(f);
const value=(f,d='')=>{const i=argv.indexOf(f);return i>=0?argv[i+1]||d:d;};
const project=value('--project',process.env.GOOGLE_CLOUD_PROJECT||process.env.GCLOUD_PROJECT||process.env.FIREBASE_PROJECT||'gestordepesquisa');
const apply=has('--apply');
const dryRun=has('--dry-run')||!apply;
const confirm=value('--confirm','');
if(has('--dry-run')&&has('--apply')){console.error('Use apenas um modo: --dry-run ou --apply.');process.exit(1);}
if(apply&&confirm!==project){console.error(`Aplicação bloqueada. Use --apply --confirm ${project} --project ${project}.`);process.exit(1);}
function hasLocalCredentials(){
  if(process.env.GOOGLE_APPLICATION_CREDENTIALS&&fs.existsSync(process.env.GOOGLE_APPLICATION_CREDENTIALS))return true;
  if(process.env.FIREBASE_CONFIG)return true;
  const home=process.env.HOME||process.env.USERPROFILE||'';
  return !!home&&fs.existsSync(`${home}/.config/gcloud/application_default_credentials.json`);
}
function adcHelp(){return `Credenciais locais do Google Cloud não encontradas.\nOpções:\n\n1. Rode: gcloud auth application-default login\n\n2. Ou execute a limpeza pelo painel admin usando a Function purgeProductionDemoData.\n\n3. Ou defina GOOGLE_APPLICATION_CREDENTIALS apontando para uma service account.`;}
function isDemoRecord(row){const text=JSON.stringify(row||{}).toLowerCase();return row?.isDemo===true||row?.demo===true||row?.fixture===true||row?.sample===true||row?.seeded===true||text.includes('survey_demo')||text.includes('empresa-exemplo')||text.includes('empresa exemplo')||text.includes('demo-token')||text.includes('projeto antigo')||(text.includes('maturidade organizacional 2026')&&text.includes('empresa exemplo'));}
const forbidden=url=>/survey_demo|empresa-exemplo|demo-token|tokenHash=/i.test(String(url||''));
async function main(){
  if(!hasLocalCredentials()){console.log(adcHelp());process.exit(0);}
  let admin;try{admin=require('firebase-admin');}catch(_){console.error('firebase-admin não disponível. Rode npm install antes de aplicar.');process.exit(1);}
  if(!admin.apps.length)admin.initializeApp({projectId:project});
  const db=admin.firestore();const TS=admin.firestore.FieldValue.serverTimestamp;
  const report={ok:true,dryRun,apply,project,matched:{surveys:[],forms:[],companies:[],organizations:[],invitations:[],responses:[]},actions:[],totals:{surveys:0,forms:0,companies:0,organizations:0,invitations:0,responses:0},demoUrlsFound:[]};
  const batch=db.batch();let writes=0;const add=(ref,patch,action)=>{report.actions.push(action);if(apply){batch.set(ref,patch,{merge:true});writes++;}};
  for(const name of ['surveys','forms','organizations','companies','invitations','responses']){
    const snap=await db.collection(name).get();
    for(const doc of snap.docs){const row={id:doc.id,...doc.data()};[row.publicUrl,row.publicLink,row.url,row.link,row.shareUrl].filter(forbidden).forEach(()=>report.demoUrlsFound.push({collection:name,id:doc.id,url:'[blocked-demo-url]'}));if(!isDemoRecord(row))continue;report.matched[name].push(doc.id);report.totals[name]++;if(name==='responses')continue;if(name==='surveys')add(doc.ref,{status:'archived',archived:true,deleted:true,revoked:true,revokedAt:TS(),featuredOnHome:false,isFeatured:false,homeFeatured:false,visibleOnHome:false,homePinned:false,visibility:'private',updatedAt:TS()},{collection:name,id:doc.id,action:'archive_revoke_hide_survey'});else if(name==='invitations')add(doc.ref,{status:'revoked',revoked:true,deleted:true,updatedAt:TS()},{collection:name,id:doc.id,action:'revoke_invitation'});else add(doc.ref,{status:'archived',archived:true,deleted:true,updatedAt:TS()},{collection:name,id:doc.id,action:'archive_demo'});}
  }
  if(writes)await batch.commit();
  console.log(JSON.stringify(report,null,2));
}
main().catch(err=>{const msg=String(err&&err.message||err);if(/Could not load the default credentials|Application Default Credentials|credential/i.test(msg)){console.log(adcHelp());process.exit(0);}console.error(`Falha controlada na limpeza demo: ${msg}`);process.exit(1);});
