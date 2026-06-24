function maskEmail(email=''){const [u,d]=String(email).split('@');if(!d)return String(email).slice(0,2)+'***';return `${u.slice(0,2)}***@${d}`}
function maskSensitiveData(data={}){return JSON.parse(JSON.stringify(data,(k,v)=>/pass|token|authorization|html/i.test(k)?'[masked]':(/email|to/i.test(k)&&typeof v==='string'?maskEmail(v):v)))}
function log(level,message,meta={}){const safe=maskSensitiveData(meta);console[level==='error'?'error':'log'](JSON.stringify({time:new Date().toISOString(),level,message,...safe}))}
module.exports={maskEmail,maskSensitiveData,log};
