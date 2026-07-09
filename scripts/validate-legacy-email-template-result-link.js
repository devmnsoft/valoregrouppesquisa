#!/usr/bin/env node
const {read,assert}=require('./validate-common-public-token');const s=read('functions/index.js');assert(/Valora Insight™/.test(s),'Valora Insight title missing');assert(/Acessar minha devolutiva/.test(s),'premium CTA missing');assert(/buildResultEmailText/.test(s),'text email missing');console.log('ok');
