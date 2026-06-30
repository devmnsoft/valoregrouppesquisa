const {assert,has,done}=require('./_legacy-validator-lib');
['buildCertificateData','renderCertificateHtml','renderCertificateSection','downloadCertificate','printCertificate','buildCertificateValidationCode','buildCertificateValidationUrl','maskedEmail','Código de validação'].forEach(x=>assert(has('app.js',x),`certificate missing ${x}`));done('legacy certificate complete');
