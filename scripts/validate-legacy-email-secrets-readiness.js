const fs=require('fs');
const s=fs.readFileSync('functions/index.js','utf8');
for(const x of ["defineSecret('SMTP_HOST')","defineSecret('SMTP_PORT')","defineSecret('SMTP_USER')","defineSecret('SMTP_PASSWORD')","defineSecret('SMTP_FROM_EMAIL')","defineSecret('SMTP_FROM_NAME')","defineSecret('SMTP_REPLY_TO')","defineSecret('PUBLIC_APP_URL')",'EMAIL_SECRETS','emailConfig()','assertEmailConfigReady']){
  if(!s.includes(x))throw new Error('email secrets ausente: '+x);
}
console.log('legacy email secrets readiness: PASS');
