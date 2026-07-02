const {read,must}=require('./legacy-public-final-validator-common');const a=read('app.js');
must('public form marker exists',/data-public-survey-form/.test(a));
must('hidden surveyId exists',/<input type="hidden" name="surveyId" value="\$\{esc\(sid\)\}">/.test(a));
must('hidden token exists',/<input type="hidden" name="token" value="\$\{esc\(token\)\}">/.test(a));
must('hidden org exists',/<input type="hidden" name="org" value="\$\{esc\(orgSlug\|\|''\)\}">/.test(a));
must('context fallback reads form hidden fields',/formEl\?\.querySelector\('\[name="surveyId"\]'\)\?\.value/.test(a)&&/formEl\?\.querySelector\('\[name="token"\]'\)\?\.value/.test(a));
must('context fallback reads dataset and route',/formEl\?\.dataset\?\.surveyId/.test(a)&&/route\.surveyId/.test(a));
