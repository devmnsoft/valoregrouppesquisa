#!/usr/bin/env node
const fs=require('fs');
function read(f){return fs.readFileSync(f,'utf8');}
function assert(c,m){if(!c){console.error('FAIL:',m);process.exitCode=1;}}
const repo=read('firebase-repository.js'), norm=read('data-normalization.js'), app=read('app.js'), fn=read('functions/index.js');
assert(/function isDeletedRecord/.test(norm)&&/function isActiveForm/.test(norm)&&/function isActiveSurvey/.test(norm),'active/deleted helpers missing');
assert(/async function loadForms\(companyId,options=\{\}\)/.test(repo)&&/rows\.filter\(isActiveForm\)/.test(repo),'loadForms must filter active forms by default');
assert(/async function loadSurveys\(companyId,options=\{\}\)/.test(repo)&&/rows\.filter\(row=>!isDeletedRecord\(row\)\)/.test(repo),'loadSurveys must filter deleted surveys by default');
assert(/deleted_survey/.test(fn)&&/archived_survey/.test(fn)&&/closed_survey/.test(fn)&&/revoked/.test(fn),'featured home reject reasons incomplete');
assert(/featuredOnHome:false,isFeatured:false,homeFeatured:false,visibleOnHome:false,revoked:true/.test(fn),'adminDeleteSurvey must remove home flags and revoke');
assert(/removedFromActiveList:true/.test(fn),'delete functions must report removal from active list');
assert(/invalidateFeaturedHomeSurveyCache/.test(repo)&&/featuredHomeSurveyPromise/.test(repo),'featured home cache invalidation missing');
assert(/survey_unavailable/.test(fn)&&/friendlyMessage:'Esta pesquisa foi encerrada/.test(fn),'validateSurveyLink unavailable handling missing');
assert(/loadForms\(\)\.filter|scopedForms/.test(app)&&/loadSurveys\(\)\.filter|scopedSurveys/.test(app),'admin table loaders not present');
if(!process.exitCode) console.log('Soft delete filtering validation passed.');
