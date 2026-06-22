const fs=require('fs'),vm=require('vm');
const context={window:{ValoraState:{plans:[],forms:[],surveys:[],responses:[],settings:{},chatbotSession:null},ValoraConfig:{FIREBASE_ENABLED:false,ENABLE_CLOUD_FUNCTIONS:false}},location:{hash:'#home',href:'https://local/#home'},URL,console};
context.window.open=()=>{}; vm.createContext(context);
vm.runInContext(fs.readFileSync('chatbot-knowledge-base.js','utf8'),context);
vm.runInContext(fs.readFileSync('chatbot-service.js','utf8'),context);
const bot=context.window.ValoraChatbot;
const qs=['quais são os planos?','qual plano é melhor para 500 respostas?','tem plano grátis?','o que é Valora Insight?','como interpreto meu resultado?','como baixo certificado?','meu link expirou','esqueci minha senha','quero falar com uma pessoa'];
let ok=true; for(const q of qs){const r=bot.generateAnswer(q); const a=r.answer||''; const pass=a.trim()&&!/undefined|NaN/.test(a)&&(/próximo passo|proximo passo|Posso|atendimento|atendente|suporte/i.test(a))&&r.actions.some(x=>x.type==='support'); console.log(`${pass?'✅':'❌'} ${q} -> ${r.intent}`); if(!pass){console.log(a); ok=false;}}
if(!ok)process.exit(1);
