'use strict';
const crypto=require('crypto');
function sha256(value){return crypto.createHash('sha256').update(String(value||''),'utf8').digest('hex');}
function timingSafeEqualHex(a,b){const x=Buffer.from(String(a||''),'hex'),y=Buffer.from(String(b||''),'hex');return x.length===y.length&&crypto.timingSafeEqual(x,y);}
function createToken(bytes=32){return crypto.randomBytes(bytes).toString('base64url');}
module.exports={sha256,timingSafeEqualHex,createToken};
