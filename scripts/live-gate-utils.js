const fs=require('fs');
const {spawnSync}=require('child_process');
const SECRET_RE=/(private_key|firebase service account|smtp password|smtp_password|password_hash|result_token_hash|token_hash|connection string|Host=|Username=|Password=|[A-Za-z0-9+/]{80,}={0,2})/i;
const STACK_RE=/(System\.[A-Za-z]+Exception| at [\w.<>]+\(|Traceback \(most recent call last\)|<html[\s>]|<!doctype html)/i;
function mkdirReports(){fs.mkdirSync('reports',{recursive:true});}
function writeJson(file,obj){mkdirReports();fs.writeFileSync(file,JSON.stringify(obj,null,2));}
function writeMd(file,title,lines){fs.writeFileSync(file,[`# ${title}`,'',...lines,''].join('\n'));}
function assert(cond,msg){if(!cond) throw new Error(msg);}
function baseUrl(){const v=process.env.VALORA_API_BASE_URL||process.env.API_BASE_URL||'http://localhost:5080'; return v.replace(/\/$/,'');}
function safeText(t){assert(!SECRET_RE.test(t),`resposta vazou marcador sensível`); assert(!STACK_RE.test(t),`resposta retornou HTML/stack trace`);}
async function raw(base,method,route,body,headers={}){const started=Date.now(); const r=await fetch(base+route,{method,headers:{...(body?{'content-type':'application/json'}:{}),'x-correlation-id':'sprint32-live-gate',...headers},body:body?JSON.stringify(body):undefined}); const buf=Buffer.from(await r.arrayBuffer()); const h=Object.fromEntries(r.headers.entries()); return {route,method,status:r.status,durationMs:Date.now()-started,buffer:buf,text:buf.toString('utf8'),headers:h,contentType:h['content-type']||''};}
async function requestJson(base,method,route,body,headers={}){const r=await raw(base,method,route,body,headers); safeText(r.text); assert(/application\/json/i.test(r.contentType),`${method} ${route} deve retornar application/json, retornou ${r.contentType||'sem content-type'}`); try{r.json=r.text?JSON.parse(r.text):null;}catch{assert(false,`${method} ${route} não retornou JSON válido`);} return r;}
async function requestAny(base,method,route,body,headers={}){const r=await raw(base,method,route,body,headers); if(/application\/json|text\//i.test(r.contentType)) safeText(r.text); else assert(/application\/pdf|image\/png/i.test(r.contentType),`${method} ${route} content-type não aceito: ${r.contentType}`); if(/application\/json/i.test(r.contentType)) r.json=JSON.parse(r.text||'null'); return r;}
async function requestBinary(base,method,route,expectedContentType,body,headers={}){const r=await raw(base,method,route,body,headers); assert(new RegExp(expectedContentType.replace('/','\\/'),'i').test(r.contentType),`${method} ${route} deve retornar ${expectedContentType}, retornou ${r.contentType||'sem content-type'}`); assert(r.buffer.length>0,`${method} ${route} retornou corpo vazio`); return r;}
const request=requestJson;
function summarize(s){return (s||'').split('\n').slice(-30).join('\n').slice(0,4000);}
function run(cmd,args,opts={}){const started=Date.now(); const r=spawnSync(cmd,args,{encoding:'utf8',shell:process.platform==='win32',...opts}); return {cmd:[cmd,...args].join(' '),status:r.status,durationMs:Date.now()-started,stdout:summarize(r.stdout),stderr:summarize(r.stderr)};}
module.exports={assert,baseUrl,request,requestJson,requestAny,requestBinary,writeJson,writeMd,run,SECRET_RE,STACK_RE,safeText};
