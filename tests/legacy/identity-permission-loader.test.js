'use strict';
const test=require('node:test');const assert=require('node:assert/strict');const fs=require('node:fs');
const app=fs.readFileSync('app.js','utf8'),repo=fs.readFileSync('firebase-repository.js','utf8'),config=fs.readFileSync('config.js','utf8'),local=fs.readFileSync('local-repository.js','utf8');
test('identidade e contato oficial têm fonte única',()=>{assert.match(config,/productName: 'Valora Insight™'/);assert.match(config,/whatsappDigits: '5591992545353'/);assert.doesNotMatch(app,/Valora Pulse/i);});
test('migração local preserva e copia a chave antiga',()=>{assert.match(config,/LEGACY_STORE_KEYS/);assert.match(local,/localStorage\.setItem\(storeKey,legacy\)/);});
test('hidratação usa manifesto, escopo e isolamento de falhas',()=>{for(const role of ['admin_valora','consultor_valora','empresa_admin','gestor_pesquisa','analista_resultados','gestor_area','participante','convidado_externo'])assert.match(repo,new RegExp(role));assert.match(repo,/Promise\.allSettled\(tasks\)/);assert.match(repo,/skipped_by_permission/);assert.match(repo,/participantUid/);assert.match(repo,/department/);});
test('erros globais não expõem operação assíncrona nem mensagem Firestore antiga',()=>{assert.doesNotMatch(app,/a operação assíncrona/);assert.doesNotMatch(repo,/Seu perfil não possui permissão para acessar estes dados/);});
