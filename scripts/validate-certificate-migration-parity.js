const fs=require('fs'); const sql=fs.readFileSync('scriptbd_completo.sql','utf8');
for (const x of ['valorapesquisa.certificates','valorapesquisa.certificate_validations','validation_code','masked_email']) if(!sql.includes(x)) throw new Error(`Certificado SQL ausente: ${x}`);
const api=fs.readFileSync('backend/Valora.Api/Controllers/CertificatesController.cs','utf8'); for (const x of ['Validate','Generate','Get']) if(!api.includes(x)) throw new Error(`Endpoint certificado ausente: ${x}`);
if(!fs.existsSync('backend/Valora.Web/Views/Certificates/Details.cshtml')||!fs.existsSync('backend/Valora.Web/Views/Certificates/Validate.cshtml')) throw new Error('Views de certificado ausentes.');
console.log('Paridade de certificados validada.');
