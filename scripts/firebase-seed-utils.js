'use strict';

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const EXPORT_KEYS = ['settings','modules','plans','companies','users','forms','surveys','responses','invitations','invoices','actionPlans','notifications','knowledgeBase','supportCategories','supportSlaPolicies','supportTickets','supportMessages','integrations','webhooks','apiKeys','logs'];
const COLLECTIONS = ['settings','modules','plans','organizations','companies','users','forms','surveys','responses','invitations','invoices','actionPlans','notifications','knowledgeBase','supportCategories','supportSlaPolicies','supportTickets','supportMessages','integrations','webhooks','apiKeys','logs'];
const SECRET_KEYS = /password|senha|secret|token|apikey|apiKey|privateKey|serviceAccount|smtpPassword|telegram|botToken|authorization|credential/i;
const PUBLIC_EMAIL_KEYS = new Set(['senderEmail','contactEmail','email','supportEmail']);

function stamp(){return new Date().toISOString().replace(/[-:]/g,'').slice(0,13).replace('T','-');}
function readJson(file){return JSON.parse(fs.readFileSync(file,'utf8'));}
function writeJson(file,obj){fs.mkdirSync(path.dirname(file),{recursive:true});fs.writeFileSync(file,JSON.stringify(obj,null,2));}
function parseArgs(argv=process.argv.slice(2)){const out={};for(let i=0;i<argv.length;i++){const a=argv[i];if(!a.startsWith('--'))continue;const k=a.slice(2);const n=argv[i+1];out[k]=(!n||n.startsWith('--'))?true:n;if(n&&!n.startsWith('--'))i++;}return out;}
function idOf(x,prefix='doc'){return String(x?.id||x?.uid||x?.key||`${prefix}_${crypto.randomUUID()}`);}
function sha256(v){return crypto.createHash('sha256').update(String(v)).digest('hex');}
function maskDocument(v){const d=String(v||'').replace(/\D/g,'');if(d.length<=4)return v||'';return `${d.slice(0,3)}***${d.slice(-2)}`;}
function looksDemo(row){return /exemplo|demo|teste|sample/i.test([row?.name,row?.publicName,row?.legalName,row?.email].filter(Boolean).join(' '));}
function sanitizeValue(value, opts={}, key=''){
  if(Array.isArray(value))return value.map(v=>sanitizeValue(v,opts,key));
  if(value&&typeof value==='object'){
    const o={};
    for(const [k,v] of Object.entries(value)){
      if(SECRET_KEYS.test(k)&&!PUBLIC_EMAIL_KEYS.has(k)&&!['tokenHash','secretHash','keyHash','resultToken'].includes(k))continue;
      if(k==='password')continue;
      if(k==='document'&&opts.maskDemoDocuments)o[k]=maskDocument(v);
      else o[k]=sanitizeValue(v,opts,k);
    }
    return o;
  }
  return value;
}
function sanitizeUser(u){const {password,senha,...rest}=u||{};return sanitizeValue({id:u?.id,uid:u?.uid,name:u?.name,email:u?.email,role:u?.role,companyId:u?.companyId,phone:u?.phone,status:u?.status,department:u?.department,position:u?.position,permissions:u?.permissions,receivesEmail:u?.receivesEmail,portalAccess:u?.portalAccess,createdAt:u?.createdAt,updatedAt:u?.updatedAt},{});}
function normalizeExport(raw, opts={}){
  const data = raw?.data || raw || {};
  const out={version:raw?.version||'local-export-v1',exportedAt:raw?.exportedAt||new Date().toISOString(),source:raw?.source||'localStorage',storeKey:raw?.storeKey||'valoraPulseFinal800',data:{}};
  for(const k of EXPORT_KEYS){
    if(k==='users')out.data.users=(data.users||[]).map(sanitizeUser);
    else if(k==='responses')out.data.responses=opts.includeResponses===false?[]:sanitizeValue(data.responses||[],opts);
    else if(k==='settings')out.data.settings=sanitizeValue(data.settings||{},opts);
    else out.data[k]=sanitizeValue(data[k]||(Array.isArray(data[k])?[]:[]),opts);
  }
  if(opts.maskDemoDocuments){
    out.data.companies=(out.data.companies||[]).map(c=>looksDemo(c)?{...c,document:maskDocument(c.document)}:c);
  }
  return out;
}
function mapCompany(c){return {id:idOf(c,'org'),name:c.name||c.publicName||'',legalName:c.legalName||c.name||'',publicName:c.publicName||c.name||'',slug:c.slug||'',document:c.document||'',email:c.email||'',phone:c.phone||'',status:c.status||c.subscription?.status||'active',planId:c.planId||c.subscription?.planId||'',subscription:c.subscription||{},limitsOverride:c.limitsOverride||{},brand:c.brand||{},settings:c.settings||{},createdAt:c.createdAt||new Date().toISOString(),updatedAt:c.updatedAt||new Date().toISOString()};}
function collectionEntries(exportObj, opts={}){
  const d=exportObj.data||{};const entries=[];
  const add=(collection,id,data)=>entries.push({collection,id,data});
  for(const c of d.companies||[]){const org=mapCompany(c);add('organizations',org.id,org);if(opts.legacyCompanies)add('companies',org.id,org);}
  for(const k of ['modules','plans','forms','surveys','invitations','invoices','actionPlans','notifications','knowledgeBase','supportCategories','supportSlaPolicies','supportTickets','supportMessages','integrations','webhooks','apiKeys','logs'])for(const row of d[k]||[])add(k,idOf(row,k),row);
  if(opts.includeResponses)for(const row of d.responses||[])add('responses',idOf(row,'resp'),{...row,migratedFromLocal:true,isDemo:!!opts.markResponsesDemo});
  if(d.settings&&typeof d.settings==='object'){
    if(d.settings.public||Object.values(d.settings).some(v=>v&&typeof v==='object'&&!Array.isArray(v)))for(const [id,val] of Object.entries(d.settings))add('settings',id,sanitizeValue(val));
    else add('settings','public',sanitizeValue(d.settings));
  }
  return entries;
}
module.exports={EXPORT_KEYS,COLLECTIONS,parseArgs,readJson,writeJson,stamp,normalizeExport,collectionEntries,idOf,sha256};
