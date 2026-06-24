const cors=require('cors');const {config}=require('./config');
const buckets=new Map();
function validateOrigin(req,res,next){const o=req.get('origin');if(!o)return next();if(config.allowedOrigins.includes(o))return next();return res.status(403).json({ok:false,error:'Origem não autorizada.'});}
function corsMiddleware(){return cors({origin(origin,cb){if(!origin||config.allowedOrigins.includes(origin))return cb(null,true);cb(new Error('Origem não autorizada.'));},credentials:false,methods:['GET','POST','OPTIONS'],allowedHeaders:['Authorization','Content-Type','X-Valora-Client']});}
function validateApiToken(req,res,next){if(!config.apiToken||config.apiToken==='trocar-por-token-forte')return res.status(503).json({ok:false,error:'Gateway sem token seguro configurado.'});const h=req.get('authorization')||'';if(h!==`Bearer ${config.apiToken}`)return res.status(401).json({ok:false,error:'Token ausente ou inválido.'});next();}
function validateJsonPayload(req,res,next){if(['POST','PUT','PATCH'].includes(req.method)&&!String(req.get('content-type')||'').includes('application/json'))return res.status(415).json({ok:false,error:'Content-Type deve ser application/json.'});next();}
function rateLimitBasic(req,res,next){const key=req.ip;const now=Date.now();const b=buckets.get(key)||[];const fresh=b.filter(t=>now-t<60000);fresh.push(now);buckets.set(key,fresh);if(fresh.length>120)return res.status(429).json({ok:false,error:'Muitas requisições. Tente novamente em instantes.'});next();}
function maskSensitiveData(req,_res,next){if(req.body?.html)req.body.html='[html recebido e omitido por segurança]';next();}
module.exports={validateOrigin,corsMiddleware,validateApiToken,validateJsonPayload,rateLimitBasic,maskSensitiveData};
