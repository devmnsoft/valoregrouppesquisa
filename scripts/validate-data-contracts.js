#!/usr/bin/env node
'use strict';
const fs=require('fs');const path=require('path');const root=path.resolve(__dirname,'..');
const scan=['app.js','chatbot-service.js','analytics-service.js','report-service.js'];
const unsafe=[/state\.settings\.faq\.map/g,/state\.(plans|surveys|responses|knowledgeBase)\.map/g,/\.participant\.name/g,/\.participant\.email/g,/state\.settings\.faq\.map/g];
const allowed=['responseParticipantLabel','responseParticipantEmail','normalizeFaqItems','asArray','normalizePlan','normalizeSurvey','normalizeResponseSafe','buildCertificateViewModel','deepClone(form','renderQuestionCard','renderQuestionInput'];
const errors=[];
for(const f of scan){const text=fs.existsSync(path.join(root,f))?fs.readFileSync(path.join(root,f),'utf8'):'';text.split(/\r?\n/).forEach((line,i)=>{if(allowed.some(a=>line.includes(a)))return;for(const re of unsafe){re.lastIndex=0;if(re.test(line))errors.push(`${f}:${i+1}: ${line.trim().slice(0,180)}`);}});}
if(errors.length){console.error('Contratos de dados violados:\n'+errors.join('\n'));process.exit(2);}console.log('Contratos de dados OK.');
