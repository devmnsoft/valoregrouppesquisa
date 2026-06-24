const {config}=require('./config');
const buckets=new Map();
function jsonError(res,status,code,message){return res.status(status).type('application/json').json({ok:false,status:code,message,code})}
function validateOrigin(req,res,next){const origin=req.get('origin');if(!origin||config.allowedOrigins.includes(origin))return next();return jsonError(res,403,'origin-not-allowed','Origem não autorizada.')}
function validateApiToken(req,res,next){const h=req.get('authorization')||'';if(!config.apiToken)return jsonError(res,503,'token-not-configured','Gateway sem token configurado.');if(!h.startsWith('Bearer '))return jsonError(res,401,'missing-token','Token ausente.');if(h!==`Bearer ${config.apiToken}`)return jsonError(res,401,'invalid-token','Token inválido.');next()}
function validateJsonPayload(req,res,next){if(req.method==='GET')return next();if(!String(req.get('content-type')||'').toLowerCase().includes('application/json'))return jsonError(res,415,'invalid-content-type','Use Content-Type application/json.');next()}
function rateLimitBasic(req,res,next){const key=req.ip||'local',now=Date.now(),win=60000,max=60;const b=buckets.get(key)||{n:0,t:now};if(now-b.t>win){b.n=0;b.t=now}b.n++;buckets.set(key,b);if(b.n>max)return jsonError(res,429,'rate-limit','Muitas requisições.');next()}
module.exports={validateOrigin,validateApiToken,validateJsonPayload,rateLimitBasic,jsonError};
