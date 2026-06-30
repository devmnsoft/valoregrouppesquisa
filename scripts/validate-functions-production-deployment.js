#!/usr/bin/env node
const {read,assert,finish,fs}=require('./validate-utils-s57');
const f=read('functions/index.js'), pkg=JSON.parse(read('functions/package.json'));
assert(/exports\.sendEmail\s*=/.test(f),'sendEmail exportado');
assert(/exports\.getEmailStatus\s*=/.test(f),'getEmailStatus exportado');
assert(/SMTP_PASSWORD/.test(f),'SMTP_PASSWORD usado');
assert(/nodemailer/.test(f),'nodemailer usado');
assert(!/console\.(log|error|warn)\([^)]*SMTP_PASSWORD/i.test(f),'senha SMTP não é logada diretamente');
assert(Boolean(pkg.dependencies?.nodemailer||pkg.devDependencies?.nodemailer),'nodemailer no package functions');
assert(fs.existsSync('FUNCTIONS_BLAZE_DEPLOYMENT.md'),'documentação de deploy existe');
finish();
