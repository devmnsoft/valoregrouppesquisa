require('dotenv').config();
const path=require('path');
const bool=v=>String(v).toLowerCase()==='true';
const list=v=>String(v||'').split(',').map(x=>x.trim()).filter(Boolean);
const config={env:process.env.NODE_ENV||'development',port:Number(process.env.PORT||8097),allowedOrigins:list(process.env.ALLOWED_ORIGINS||'https://valoragroup.mnsoft.com.br,http://localhost:8095'),apiToken:process.env.GATEWAY_API_TOKEN||'',payloadLimit:'128kb',smtp:{host:process.env.SMTP_HOST||'',port:Number(process.env.SMTP_PORT||587),secure:bool(process.env.SMTP_SECURE),user:process.env.SMTP_USER||'',pass:process.env.SMTP_PASS||'',fromName:process.env.SMTP_FROM_NAME||'Valora Group',fromEmail:process.env.SMTP_FROM_EMAIL||''},whatsapp:{enabled:bool(process.env.WHATSAPP_ENABLED),accessToken:process.env.WHATSAPP_ACCESS_TOKEN||'',phoneNumberId:process.env.WHATSAPP_PHONE_NUMBER_ID||'',apiVersion:process.env.WHATSAPP_API_VERSION||'v20.0'},logDir:path.resolve(process.cwd(),process.env.LOG_DIR||'./logs')};
module.exports={config};
