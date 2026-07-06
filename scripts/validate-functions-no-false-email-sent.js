const fs=require('fs');
const s=fs.readFileSync('functions/index.js','utf8');
for(const x of ['function normalizeSendMailResult','messageId&&acceptedTarget&&!rejectedTarget',"status:'sent'",'acceptedTarget','rejectedTarget','smtp_not_accepted'])if(!s.includes(x))throw new Error('proteção contra sent falso ausente: '+x);
if(!/const info=await transporter\.sendMail[\s\S]*const normalized=normalizeSendMailResult\(info,to\)/.test(s))throw new Error('sendMail não é normalizado antes do status sent');
console.log('functions no false email sent: PASS');
