#!/usr/bin/env node
'use strict';
const fs=require('fs');
function fail(message){console.error(`[functions:secrets-readiness] FAIL: ${message}`);process.exit(1);}
function pass(message){console.log(`[functions:secrets-readiness] PASS: ${message}`);}
const index=fs.readFileSync('functions/index.js','utf8');
const telegram=fs.readFileSync('functions/utils/telegram.js','utf8');
if(!/defineSecret\(['"]SMTP_PASSWORD['"]\)/.test(index))fail('SMTP_PASSWORD deve existir como defineSecret em functions/index.js. Configure com: firebase functions:secrets:set SMTP_PASSWORD --project gestordepesquisa');
if(/defineSecret\(['"]TELEGRAM_BOT_TOKEN['"]\)/.test(index))fail('TELEGRAM_BOT_TOKEN não deve ser declarado como defineSecret global em functions/index.js');
if(/defineSecret\(['"]TELEGRAM_CHAT_ID['"]\)/.test(index))fail('TELEGRAM_CHAT_ID não deve ser secret obrigatório; use variável opcional TELEGRAM_CHAT_ID');
const critical=['submitSurveyResponse','sendResultEmail','createClient','createUser','getPublicResult','getEmailStatus','sendEmail','updateClient','updateUserProfile','sendUserInvite','repairFreeSurveyPublicLink'];
for(const name of critical){
  const re=new RegExp(`exports\\.${name}\\s*=\\s*on(Call|Request|Schedule)\\s*\\(([^)]*)`, 's');
  const m=index.match(re);
  if(!m)fail(`Function crítica ${name} não encontrada`);
  if(/TELEGRAM_BOT_TOKEN|TELEGRAM_CHAT_ID/.test(m[2]))fail(`${name} lista Telegram em secrets/opções`);
}
for(const name of ['sendEmail','sendResultEmail','getEmailStatus']){
  const re=new RegExp(`exports\\.${name}\\s*=\\s*onCall\\s*\\(\\s*\\{[^}]*secrets\\s*:\\s*\\[\\s*SMTP_PASSWORD\\s*\\]`, 's');
  if(!re.test(index))fail(`${name} deve continuar com secrets:[SMTP_PASSWORD]`);
}
if(!/TELEGRAM_ENABLED/.test(telegram)||!/telegram_disabled/.test(telegram)||!/telegram_not_configured/.test(telegram)||!/sendTelegramNotification/.test(telegram))fail('functions/utils/telegram.js deve tratar Telegram como opcional');
const all=index+'\n'+telegram;
if(/\d{8,12}:[A-Za-z0-9_-]{30,}/.test(all))fail('Possível token Telegram hardcoded encontrado');
if(/SMTP_PASSWORD\s*=\s*['"][^'"]+['"]|pass\s*:\s*['"][^'"]{8,}['"]/.test(all))fail('Possível senha SMTP hardcoded encontrada');
pass('SMTP_PASSWORD obrigatório para e-mail; Telegram opcional e ausente das Functions críticas.');
