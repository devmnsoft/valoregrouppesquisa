#!/usr/bin/env node
const {read,assert}=require('./validate-common-public-token');const s=read('functions/index.js')+'\n'+read('app.js');assert(/exports\.requestNewResultLink/.test(s),'requestNewResultLink callable missing');assert(/participant_email_mismatch/.test(s),'participant email check missing');assert(/requestNewResultLink/.test(read('app.js')),'front action missing');console.log('ok');
