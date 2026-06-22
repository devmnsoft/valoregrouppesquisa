#!/usr/bin/env node
const fs=require('fs');const vm=require('vm');
const sandbox={console,location:{hash:'#home',href:'https://example.test/index.html#home'},sessionStorage:{_:{},getItem(k){return this._[k]||null},setItem(k,v){this._[k]=String(v)}},window:{},URL,Date,Math};
sandbox.window=sandbox; sandbox.window.open=()=>{}; sandbox.window.ValoraConfig={FIREBASE_ENABLED:true,FIREBASE_PLAN:'Spark',ENABLE_CLOUD_FUNCTIONS:false}; sandbox.window.ValoraLogger={info(){}};
sandbox.window.ValoraState={session:null,users:[],companies:[],plans:[],forms:[],surveys:[],responses:[],actionPlans:[],knowledgeBase:[]};
vm.createContext(sandbox);
for(const file of ['chatbot-knowledge-base.js','chatbot-service.js']) vm.runInContext(fs.readFileSync(file,'utf8'),sandbox,{filename:file});
const bot=sandbox.window.ValoraChatbot;
const questions=['quais são os planos?','qual plano é melhor para 500 respostas?','o que é Valora Insight?','como interpretar meu resultado?','o que significa em estruturação?','como baixo certificado?','quero falar com uma pessoa','meu link expirou','esqueci minha senha'];
let failures=[];
for(const q of questions){const r=bot.generateAnswer(q,bot.getValoraBotContext());const text=String(r.answer||'');const bad=!text||/undefined|NaN/.test(text)||text.length<80||/^não entendi\.?$/i.test(text);const hasAction=Array.isArray(r.actions)&&r.actions.length>0;const needsSupport=/pessoa|expirou|senha|suporte/.test(q);if(bad)failures.push(`${q}: resposta inválida (${text.slice(0,60)})`);if(!hasAction)failures.push(`${q}: sem ação seguinte`);if(needsSupport&&!r.actions.some(a=>/atendente|suporte/i.test(a.label)))failures.push(`${q}: não oferece suporte humano`);console.log(`PASS ${q} -> ${r.intent} (${text.length} chars)`);} 
if(failures.length){console.error('\nFalhas:\n- '+failures.join('\n- '));process.exit(1);}console.log('\nValoraBot validation: PASS');
