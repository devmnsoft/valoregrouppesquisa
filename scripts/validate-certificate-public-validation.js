#!/usr/bin/env node
const {has}=require('./sprint48-validator-lib');
has('backend/Valora.Api/Controllers/CertificatesController.cs',['/certificates/validate/{certificateCode}','participantEmailMasked','valid = true']);
has('backend/Valora.Web/Views/Certificates/Validate.cshtml',['Validar certificado']);
has('backend/Valora.Web/wwwroot/js/api/certificates-api.js',['/certificates/validate/']);
has('CERTIFICATE_PUBLIC_VALIDATION.md',['e-mail completo','resultToken','token_hash']);
console.log('validate-certificate-public-validation: PASS');
