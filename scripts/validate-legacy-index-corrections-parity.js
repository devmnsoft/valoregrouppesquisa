#!/usr/bin/env node
const fs=require('fs');
function must(file,patterns){const s=fs.readFileSync(file,'utf8');for(const p of patterns){if(!p.test(s)){console.error(file+' missing '+p);process.exit(1);}}}
must('app.js',[/function normalizeDateLike/,/function isSurveyExpired/,/function ensureSurveyPublicLink/,/publicToken||survey.token||survey.accessToken/,/function openAdminMobileMenu/,/data-action="toggleAdminMobileMenu"/]);
must('firebase-repository.js',[/createCompanyUser/,/mapAuthError/,/E-mail ou senha inválidos/]);
must('functions/index.js',[/function isBetween/,/isFreeOfficialSurvey/,/exports.createCompanyUser/]);
console.log('Paridade estrutural do index antigo validada.');