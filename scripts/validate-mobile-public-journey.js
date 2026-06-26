#!/usr/bin/env node
require('./validate-mobile-menu');
const fs=require('fs');const app=fs.readFileSync('app.js','utf8');
if(!/Responder diagnóstico grátis/.test(app)||!/renderTakeSurvey/.test(app)||!/certificatePng/.test(app))throw new Error('Jornada pública mobile incompleta.');
console.log('validate-mobile-public-journey: PASS');
