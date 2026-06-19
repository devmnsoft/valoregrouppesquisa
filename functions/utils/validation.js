'use strict';
const {HttpsError}=require('firebase-functions/v2/https');
function required(data,field){if(!data||data[field]===undefined||data[field]===null||String(data[field]).trim()==='')throw new HttpsError('invalid-argument',`${field} é obrigatório.`);return String(data[field]).trim();}
function asObject(v,name){if(!v||typeof v!=='object'||Array.isArray(v))throw new HttpsError('invalid-argument',`${name} deve ser um objeto.`);return v;}
function asBoolean(v){return v===true||v==='true'||v===1||v==='1';}
function sanitizeString(v,max=500){return String(v??'').trim().slice(0,max);}
function validateParticipant(p={}){return {personType:sanitizeString(p.personType||'fisica',20),name:sanitizeString(p.name,160),email:sanitizeString(p.email,180).toLowerCase(),phone:sanitizeString(p.phone,40),isWhatsapp:asBoolean(p.isWhatsapp),document:sanitizeString(p.document,40),cep:sanitizeString(p.cep,20),address:sanitizeString(p.address,240),sendEmail:asBoolean(p.sendEmail)};}
module.exports={required,asObject,asBoolean,sanitizeString,validateParticipant};
