const fs=require('fs'); const sql=fs.readFileSync('scriptbd_completo.sql','utf8');
for (const x of ['valorapesquisa.email_jobs','valorapesquisa.email_templates','result_email','valoragroup@mnsoft.com.br']) if(!sql.includes(x)) throw new Error(`E-mail SQL ausente: ${x}`);
const app=fs.readFileSync('backend/Valora.Api/appsettings.json','utf8'); if(!app.includes('valoragroup@mnsoft.com.br')||/"Password"\s*:\s*"[^"]+"/.test(app)) throw new Error('Config SMTP insegura ou incompleta.');
console.log('Paridade de e-mail validada.');
