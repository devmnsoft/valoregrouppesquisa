#!/usr/bin/env node
'use strict';
const assert=require('assert');const norm=require('../data-normalization');
const cases=[{settings:{faq:'Pergunta? Resposta'}},{settings:{faq:{pergunta1:'resposta1',pergunta2:'resposta2'}}},{settings:{faq:{items:[{question:'Q',answer:'A'}]}}},{plans:{},surveys:null,responses:'',knowledgeBase:{a:{title:'Artigo'}}}];
for(const c of cases){const s=norm.normalizeAppState(c);assert(Array.isArray(s.settings.faq));assert(Array.isArray(s.plans));assert(Array.isArray(s.surveys));assert(Array.isArray(s.responses));assert(Array.isArray(s.knowledgeBase));const homeFaq=s.settings.faq.map(i=>`${i.question} ${i.answer}`).join('\n');assert(!/undefined|NaN|\[object Object\]/.test(homeFaq));}
console.log('Render resilience OK: estados legados normalizados sem undefined/NaN/[object Object].');
