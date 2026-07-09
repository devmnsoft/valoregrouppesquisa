#!/usr/bin/env node
const {read,assert}=require('./validate-common-public-token');const s=read('functions/index.js');assert(/exports\.adminRegenerateResultLink/.test(s),'adminRegenerateResultLink missing');assert(/resultAccessRepairedBy/.test(s),'repair audit field missing');console.log('ok');
