#!/usr/bin/env node
const fs=require('fs');const s=fs.readFileSync('app.js','utf8');['buildCertificateData','renderCertificateHtml','downloadCertificate','buildCertificateValidationCode','buildCertificatePublicValidationUrl','Baixar/Imprimir Certificado','maskedEmail','validationUrl'].forEach(x=>{if(!s.includes(x))throw new Error('Ausente: '+x)});console.log('OK certificado legado.');
