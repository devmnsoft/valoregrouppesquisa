require('dotenv').config();
function bool(v){return String(v).toLowerCase()==='true'}
function num(v,d){const n=Number(v);return Number.isFinite(n)?n:d}
const config={environment:process.env.NODE_ENV||'production',port:num(process.env.PORT,8097),allowedOrigins:String(process.env.ALLOWED_ORIGINS||'https://valoragroup.mnsoft.com.br,http://localhost:8095').split(',').map(x=>x.trim()).filter(Boolean),apiToken:process.env.GATEWAY_API_TOKEN||process.env.GATEWAY_TOKEN||'',logDir:process.env.LOG_DIR||'./logs',smtp:{host:process.env.SMTP_HOST||'',port:num(process.env.SMTP_PORT,587),secure:bool(process.env.SMTP_SECURE),user:process.env.SMTP_USER||'',pass:process.env.SMTP_PASS||'',fromName:process.env.SMTP_FROM_NAME||'Valora Group',fromEmail:process.env.SMTP_FROM_EMAIL||process.env.SMTP_USER||''}};
config.emailConfigured=Boolean(config.smtp.host&&config.smtp.port&&config.smtp.user&&config.smtp.pass&&config.smtp.fromEmail);
module.exports={config};
