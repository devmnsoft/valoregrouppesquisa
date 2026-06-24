function isEmail(v){return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(String(v||''))}
function validateResultPayload(b={}){const errors=[];if(!b.responseId)errors.push('responseId');if(!b.participant||typeof b.participant!=='object')errors.push('participant');if(!isEmail(b.participant?.email))errors.push('participant.email');if(!b.links?.resultUrl)errors.push('links.resultUrl');return errors}
module.exports={isEmail,validateResultPayload};
