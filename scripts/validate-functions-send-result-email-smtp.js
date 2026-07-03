const fs=require('fs');
const s=fs.readFileSync('functions/index.js','utf8');
const start=s.indexOf('exports.sendResultEmail');
const end=s.indexOf('exports.queueResultEmail',start);
const f=s.slice(start,end);
for(const x of ['secrets:EMAIL_SECRETS','loadResultEmailContext','resultToken','PUBLIC_APP_URL','sendOneEmail','emailJobs',"status:'sent'",'pending_retry']){
  if(!f.includes(x))throw new Error('sendResultEmail SMTP incompleto: '+x);
}
if(!/nodemailer\.createTransport[\s\S]*auth:\{user:cfg\.user,pass:cfg\.pass\}/.test(s))throw new Error('nodemailer não usa cfg de secrets/env');
console.log('functions send result email smtp: PASS');
