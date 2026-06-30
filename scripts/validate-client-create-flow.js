#!/usr/bin/env node
const {read,ok,done}=require('./validate-helper');const app=read('app.js'),repo=read('firebase-repository.js');
ok(/function saveCompany\(form\)/.test(app),'saveCompany ausente');ok(/normalizeOrganization/.test(app)&&/planId/.test(app)&&/status/.test(app)&&/slug/.test(app),'normalização/plano/status/slug ausentes');ok(/collection\('organizations'\)\.doc\(\)/.test(repo),'cadastro não cria organizations');ok(/settings/.test(app+repo),'settings padrão não mapeado');ok(/audit\('Cliente cadastrado'/.test(app),'auditoria de cliente ausente');done('validate-client-create-flow');
