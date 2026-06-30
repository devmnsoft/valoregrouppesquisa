#!/usr/bin/env node
const fs=require('fs');const files=['app.js','functions/index.js'];const all=files.map(f=>fs.readFileSync(f,'utf8')).join('\n');function assert(c,m){if(!c){console.error('FAIL:',m);process.exitCode=1;}}
const text='Ao continuar, você declara que leu e concorda com o tratamento dos dados informados neste diagnóstico pela Valora Group.';
assert(all.includes(text),'new LGPD text missing');
assert(all.includes('Consentimento para participação no diagnóstico'),'LGPD title missing');
assert(all.includes('Li e concordo com o uso dos meus dados para gerar meu resultado e registrar minha participação.'),'LGPD checkbox missing');
assert(all.includes('Usamos seus dados apenas para processar o diagnóstico'),'LGPD card text missing');
assert(!all.includes('Ao responder este diagnóstico, você autoriza o tratamento dos dados informados para geração do resultado, histórico e contato relacionado às soluções da Valora Group.'),'old weak LGPD text remains');
if(!process.exitCode) console.log('LGPD text validation passed.');
