#!/usr/bin/env node
const assert=require('assert');
const {normalizeFaqItems,defaultFaq}=require('../data-normalization');
const forbidden=['id','updatedBy','updatedAt','migratedAt','source'];
function htmlFor(faq){return normalizeFaqItems(faq,defaultFaq()).map(x=>`<details class="faq-item"><summary>${x.question}</summary><p>${x.answer}</p></details>`).join('');}
function check(name,faq,expectsDefault=false){const html=htmlFor(faq);for(const key of forbidden)assert(!html.includes(`<summary>${key}</summary>`),`${name}: renderizou ${key}`);assert(!/<summary>[0-3]<\/summary>/.test(html),`${name}: renderizou índice numérico`);assert(html.includes('<summary>'),`${name}: não renderizou FAQ`);if(expectsDefault)assert(html.includes('O que é o Valora Insight™?'),`${name}: não caiu no defaultFaq`);return html;}
check('array direto',[{question:'Q',answer:'A'}]);
check('objeto numerico',{0:{question:'Q1',answer:'A1'},1:{question:'Q2',answer:'A2'},id:'settings',updatedBy:'admin',updatedAt:'...',migratedAt:'...',source:'firestore'});
check('metadados puros',{id:'settings',updatedBy:'admin',source:'firestore'},true);
console.log('validate-faq-rendering: PASS');
