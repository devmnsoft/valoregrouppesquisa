#!/usr/bin/env node
const assert=require('assert');
const {isSurveyExpired}=require('./featured-home-survey-core');
const future=new Date(Date.now()+86400000),past=new Date(Date.now()-86400000);
[{expiresAt:future},{expiresAt:future.toISOString()},{expiresAt:{seconds:Math.floor(future/1000)}},{expiresAt:{toDate:()=>future}},{isFree:true},{isFree:true,expiresAt:null},{isFree:true,expiresAt:past},{planId:'free',expiresAt:past},{title:'Diagnóstico gratuito Valora Insight',expiresAt:past}].forEach(s=>assert.equal(isSurveyExpired(s,{freeSurvey:!!s.isFree}),false));
assert.equal(isSurveyExpired({expiresAt:past}),true);
assert.equal(isSurveyExpired({isFree:true,expiresAt:past},{strict:true}),true);
console.log('Expiração robusta da pesquisa gratuita validada.');
