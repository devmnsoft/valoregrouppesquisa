#!/usr/bin/env node
'use strict';
const fs=require('fs');
const path=require('path');
const {execFileSync}=require('child_process');
let failed=false;
function fail(msg){console.error(`ERRO: ${msg}`);failed=true;}
function warn(msg){console.warn(`AVISO: ${msg}`);}
function read(file){return fs.readFileSync(file,'utf8');}
function walk(dir,out=[]){if(!fs.existsSync(dir))return out;for(const ent of fs.readdirSync(dir,{withFileTypes:true})){const full=path.join(dir,ent.name);if(ent.isDirectory()){if(['node_modules','.git','.firebase'].includes(ent.name))continue;walk(full,out);}else out.push(full);}return out;}
const trackedDist=execFileSync('git',['ls-files','dist'],{encoding:'utf8'}).trim();
if(trackedDist)fail(`dist/ não deve ser versionado:\n${trackedDist}`);
const firebaseJson=JSON.parse(read('firebase.json'));
if(firebaseJson.hosting?.public!=='dist')fail(`Firebase Hosting deve publicar somente dist/; atual=${firebaseJson.hosting?.public}`);
const csp=(firebaseJson.hosting?.headers||[]).flatMap(h=>h.headers||[]).find(h=>String(h.key).toLowerCase()==='content-security-policy')?.value||'';
if(!csp)fail('Content-Security-Policy ausente em firebase.json.');
if(/connect-src[^;]*(?:^|\s)\*(?:\s|;|$)/i.test(csp))fail('CSP contém connect-src *');
if(/script-src[^;]*(?:^|\s)\*(?:\s|;|$)/i.test(csp))fail('CSP contém script-src *');
if(/unsafe-eval/i.test(csp))fail('CSP contém unsafe-eval');
const sensitiveNames=[/^\.env(\.|$|$)/i,'serviceAccount.json','secrets.json','telegram.env','email_config.json'];
for(const f of walk('dist')){
  const base=path.basename(f), rel=f.replace(/\\/g,'/');
  if(/\.map$/i.test(base))fail(`source map publicado em ${rel}`);
  if(rel.includes('data/outbox'))fail(`outbox publicado em ${rel}`);
  if(sensitiveNames.some(x=>x instanceof RegExp?x.test(base):x.toLowerCase()===base.toLowerCase()))fail(`arquivo sensível publicado em ${rel}`);
  const txt=read(f);
  if(/TELEGRAM_BOT_TOKEN|bot\d{6,}:[A-Za-z0-9_-]{20,}|BotToken/i.test(txt))fail(`possível token Telegram em ${rel}`);
  if(/SMTP_PASSWORD|smtp_password|smtp.{0,20}password\s*[:=]\s*['"][^'"]{8,}/i.test(txt))fail(`possível senha SMTP em ${rel}`);
  if(/-----BEGIN PRIVATE KEY-----|private_key|client_email.*iam\.gserviceaccount\.com/i.test(txt))fail(`possível service account/chave privada em ${rel}`);
  if(/connect-src[^;]*(?:^|\s)\*(?:\s|;|$)/i.test(txt))fail(`connect-src * encontrado em ${rel}`);
  if(/script-src[^;]*(?:^|\s)\*(?:\s|;|$)/i.test(txt))fail(`script-src * encontrado em ${rel}`);
  if(/unsafe-eval/i.test(txt))fail(`unsafe-eval encontrado em ${rel}`);
  if(/<[^>]+\son[a-z]+\s*=/i.test(txt))fail(`possível handler inline/inseguro em ${rel}`);
}
const sourceFiles=walk('.').filter(f=>!f.startsWith('dist/')&&!f.includes('/node_modules/')&&!f.includes('/.git/'));
for(const f of sourceFiles){const rel=f.replace(/\\/g,'/');if(/(^|\/)\.env(\.|$)|serviceAccount\.json$|secrets\.json$|telegram\.env$|email_config\.json$/i.test(rel))fail(`arquivo sensível presente no repositório: ${rel}`);}
if(failed)process.exit(1);
console.log('security-check aprovado: hosting usa dist/, CSP restritiva e artefatos publicados sem source maps/segredos conhecidos.');
