#!/usr/bin/env node
const {read,assert}=require('./validate-common-public-token');const s=read('app.js');assert(/shareSurveyWhatsapp[\s\S]*preparePublicSurveyLink/.test(s),'survey WhatsApp must prepare public link');assert(/Segue o link seguro para acessar sua devolutiva Valora Insight™/.test(s),'result WhatsApp secure message missing');console.log('ok');
