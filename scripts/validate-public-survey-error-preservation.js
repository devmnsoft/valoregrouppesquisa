'use strict';
const fs=require('fs');const vm=require('vm');const assert=require('assert');
const app=fs.readFileSync('app.js','utf8');
const match=app.match(/function normalizePublicSubmitError\(err\) \{[\s\S]*?\n\}/);assert(match,'normalizador público não encontrado');
const sandbox={};vm.runInNewContext(`${match[0]};this.normalizePublicSubmitError=normalizePublicSubmitError`,sandbox);
for(const code of ['survey_not_found','invalid_public_token','form_not_found','public_form_questions_missing','survey_form_mismatch']){const actual=sandbox.normalizePublicSubmitError({code:'functions/failed-precondition',details:{code,friendlyMessage:`friendly:${code}`,diagnostics:{rejectedReasons:[code]}}});assert.strictEqual(actual.code,code);assert.deepStrictEqual(Array.from(actual.details.diagnostics.rejectedReasons),[code]);}
const unavailable=sandbox.normalizePublicSubmitError({code:'public_validation_unavailable',details:{code:'public_validation_unavailable',providerCode:'functions/unavailable'}});assert.strictEqual(unavailable.code,'public_validation_unavailable');
assert(/originalError=stateNow\.error[\s\S]*originalError\?\.code\?originalError/.test(app),'renderTakeSurvey ainda mascara erro específico');
assert(/setPublicSurveyState\(\{ status: 'ready'/.test(app)&&/public_form_questions_missing/.test(app),'contrato ready/perguntas ausente');
console.log('public survey error preservation: PASS (7 cenários)');
