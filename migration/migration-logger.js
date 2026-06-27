'use strict';
const SECRET_PATTERNS=[/(private_key|password|senha|token|client_email)\s*[:=]\s*[^\s,;}]+/gi,/-----BEGIN PRIVATE KEY-----[\s\S]*?-----END PRIVATE KEY-----/g];
function sanitize(value){let text=typeof value==='string'?value:JSON.stringify(value||{});for(const pattern of SECRET_PATTERNS)text=text.replace(pattern,'$1=[redacted]');return text;}
function event(level,message,metadata={}){const payload={ts:new Date().toISOString(),level,message,...metadata};process[level==='error'?'stderr':'stdout'].write(`${sanitize(payload)}\n`);}
module.exports={sanitize,info:(m,md)=>event('info',m,md),warn:(m,md)=>event('warn',m,md),error:(m,md)=>event('error',m,md),step:(m,md)=>event('info',m,{step:true,...md}),success:(m,md)=>event('info',m,{success:true,...md}),fail:(m,md)=>event('error',m,{fail:true,...md}),time(){const start=Date.now();return()=>Date.now()-start;}};
