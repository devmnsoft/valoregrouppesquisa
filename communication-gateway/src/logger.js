function maskEmail(email=''){const [u,d]=String(email).split('@');return d?`${u.slice(0,2)}***@${d}`:'***';}
function maskPhone(p=''){const s=String(p).replace(/\D/g,'');return s?`${s.slice(0,4)}***${s.slice(-2)}`:'***';}
function sanitize(data){const s=JSON.parse(JSON.stringify(data||{}));['SMTP_PASS','WHATSAPP_ACCESS_TOKEN','GATEWAY_API_TOKEN','password','token','html'].forEach(k=>{if(s[k])s[k]='[masked]';});return s;}
function log(level,msg,meta={}){console[level](`[${new Date().toISOString()}] ${msg}`,sanitize(meta));}
module.exports={maskEmail,maskPhone,sanitize,info:(m,x)=>log('log',m,x),warn:(m,x)=>log('warn',m,x),error:(m,x)=>log('error',m,x)};
