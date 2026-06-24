const emailRe=/^[^\s@]+@[^\s@]+\.[^\s@]+$/;const phoneRe=/^\d{10,15}$/;
const isEmail=v=>emailRe.test(String(v||''));const isPhone=v=>phoneRe.test(String(v||'').replace(/\D/g,''));
function validateResultPayload(p){const e=[];if(!p||typeof p!=='object')e.push('Payload JSON inválido.');if(!p?.responseId)e.push('responseId é obrigatório.');if(!p?.participant)e.push('participant é obrigatório.');if(p?.participant&&!isEmail(p.participant.email)&&!isPhone(p.participant.phone))e.push('Informe e-mail ou telefone válido do participante.');return e;}
function validateEmailPayload(p){const e=[];if(!isEmail(p?.to||p?.participant?.email))e.push('Destinatário de e-mail inválido.');if(!p?.subject&&!p?.templateType)e.push('Assunto ou templateType é obrigatório.');return e;}
function validateWhatsAppPayload(p){const e=[];if(!isPhone(p?.to||p?.participant?.phone))e.push('Telefone inválido.');return e;}
module.exports={isEmail,isPhone,validateResultPayload,validateEmailPayload,validateWhatsAppPayload};
