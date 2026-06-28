#!/usr/bin/env node
const fs=require('fs'); const {assert,writeJson,writeMd}=require('./live-gate-utils');
const controller=fs.readFileSync('backend/Valora.Api/Controllers/E2eFixtureController.cs','utf8');
const seed=fs.readFileSync('database/postgresql/099_seed_e2e_live_fixture.sql','utf8');
const checks=[]; function check(name,cond){checks.push({name,status:cond?'PASS':'FAIL'}); assert(cond,name);}
check('controller blocks Production',/IsProduction\(\).*return false/s.test(controller));
check('controller allows only Development Local Test or explicit flag',/EnableFixtureEndpoints/.test(controller)&&/Local/.test(controller)&&/Test/.test(controller));
check('seed uses local fixture email only',/e2e-admin@valoragroup\.local/.test(seed));
check('seed stores password_hash not clear password column',/password_hash/.test(seed)&&!/Valora!12345/.test(seed));
check('production defaults preserved',/DATA_PROVIDER:\s*'firebase'/.test(fs.readFileSync('config.js','utf8'))&&/ALLOW_API_PRODUCTION_CUTOVER:\s*false/.test(fs.readFileSync('config.js','utf8')));
const report={generatedAt:new Date().toISOString(),status:'PASS',checks}; writeJson('reports/e2e-fixture-safety.json',report); writeMd('E2E_FIXTURE_SAFETY.md','E2E Fixture Safety',[`- Status: ${report.status}`,...checks.map(c=>`- ${c.status} ${c.name}`)]); console.log('validate-e2e-fixture-safety: PASS');
