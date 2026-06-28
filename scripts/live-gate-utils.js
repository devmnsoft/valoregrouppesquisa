const fs=require('fs');
const path=require('path');
const {spawnSync}=require('child_process');
const SECRET_RE=/(private_key|firebase service account|smtp password|smtp_password|password_hash|result_token_hash|token_hash|connection string|Host=|Username=|Password=|[A-Za-z0-9+/]{80,}={0,2})/i;
const STACK_RE=/(System\.[A-Za-z]+Exception| at [\w.<>]+\(|Traceback \(most recent call last\)|<html[\s>]|<!doctype html)/i;
function mkdirReports(){fs.mkdirSync('reports',{recursive:true});}
function writeJson(file,obj){mkdirReports();fs.writeFileSync(file,JSON.stringify(obj,null,2));}
function writeMd(file,title,lines){fs.writeFileSync(file,[`# ${title}`,'',...lines,''].join('\n'));}
function assert(cond,msg){if(!cond) throw new Error(msg);}
function baseUrl(){const v=process.env.VALORA_API_BASE_URL||process.env.API_BASE_URL; assert(v,'API_BASE_URL ou VALORA_API_BASE_URL deve estar configurado para gate live.'); return v.replace(/\/$/,'');}
async function request(base,method,route,body,headers={}){const started=Date.now(); const r=await fetch(base+route,{method,headers:{'content-type':'application/json','x-correlation-id':'sprint31-live-gate',...headers},body:body?JSON.stringify(body):undefined}); const text=await r.text(); assert(!SECRET_RE.test(text),`${method} ${route} vazou marcador sensível`); assert(!STACK_RE.test(text),`${method} ${route} retornou HTML/stack trace`); let json=null; try{json=text?JSON.parse(text):null;}catch{assert(false,`${method} ${route} não retornou JSON válido`);} return {route,method,status:r.status,durationMs:Date.now()-started,json,text,headers:Object.fromEntries(r.headers.entries())};}
function summarize(s){return (s||'').split('\n').slice(-30).join('\n').slice(0,4000);}
function run(cmd,args,opts={}){const started=Date.now(); const r=spawnSync(cmd,args,{encoding:'utf8',shell:process.platform==='win32',...opts}); return {cmd:[cmd,...args].join(' '),status:r.status,durationMs:Date.now()-started,stdout:summarize(r.stdout),stderr:summarize(r.stderr)};}
module.exports={assert,baseUrl,request,writeJson,writeMd,run,SECRET_RE,STACK_RE};
