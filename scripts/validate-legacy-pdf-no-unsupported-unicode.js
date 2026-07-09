const {no}=require('./legacy-final-validator-lib');['pdf.js','report-service.js'].forEach(f=>no(f,/[█░™➡✅⚠❌💬]/,`${f} unsupported unicode`));console.log('ok');
