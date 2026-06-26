#!/usr/bin/env node
'use strict';
const fs = require('fs');
const path = require('path');
const checks = [
  ['backend/Valora.Api/Controllers/PublicSurveysController.cs', '[HttpPost("/public/surveys/{surveyId}/validate")'],
  ['backend/Valora.Api/Controllers/PublicSurveysController.cs', '[HttpPost("/public/surveys/{surveyId}/responses")'],
  ['backend/Valora.Api/Controllers/PublicResultsController.cs', '[HttpPost("/public/results/{responseId}")']
];
const errors = [];
for (const [file, needle] of checks) {
  const full = path.resolve(file);
  if (!fs.existsSync(full)) { errors.push(`${file}: arquivo ausente`); continue; }
  const text = fs.readFileSync(full, 'utf8');
  if (text.replace(/\s/g, '').length < 80) errors.push(`${file}: controller vazio ou quase vazio`);
  if (!text.includes(needle)) errors.push(`${file}: rota ausente ${needle}`);
}
if (errors.length) { console.error(errors.join('\n')); process.exit(1); }
console.log('validate-public-api-routes: PASS');
