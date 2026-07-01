#!/usr/bin/env node
const fs=require('fs');
const checks=[];
function file(p){return fs.readFileSync(p,'utf8');}
const fn=file('functions/index.js'), app=file('app.js'), repo=file('firebase-repository.js');
function check(name,ok){checks.push({name,ok});if(!ok)console.error(`FAIL ${name}`);else console.log(`OK ${name}`);}
check('preparePublicSurveyDocument returns sanitized form',/form:safeForm/.test(fn));
check('preparePublicSurveyDocument returns consistency',/consistency:\{surveyIdMatchesRequest:true/.test(fn));
check('preparePublicSurveyDocument preserves before.formId',/formId:before\.formId/.test(fn)&&!/formId:'form_valora_insight_oficial'/.test(fn.slice(fn.indexOf('async function preparePublicSurveyDocument'),fn.indexOf('function publicValidationError'))));
check('getFeaturedHomeSurvey validates form.id === survey.formId',/sanitizeFeaturedPayload[\s\S]*form\.id!==survey\.formId/.test(fn));
check('getFeaturedHomeSurvey prioritizes real manual over official fallback',/officialPenalty/.test(fn)&&/manualPriority/.test(fn)&&/sortFeaturedCandidates/.test(fn));
check('ensureOfficialFreeSurveyFallback only marks official when allowed and no real featured',/ensureOfficialFreeSurveyFallback\(\{markFeatured=false\}=\{\}\)/.test(fn)&&/const shouldFeature=markFeatured===true&&!realFeatured/.test(fn));
check('featured button calls preparePublicSurveyLink with clicked survey id',/preparePublicSurveyLink'?,\{surveyId:survey\.id,featured:true,free:true\}/.test(app));
check('front validates result survey and form ids',/data\?\.survey\?\.id!==survey\.id/.test(app)&&/data\?\.form\?\.id!==survey\.formId/.test(app));
check('publicSurveyCache stores compatible survey and form with consistency',/publicSurveyCache\.set\(survey\.id,\{survey:data\.survey,form:data\.form/.test(app)&&/consistency:data\.consistency/.test(app)&&/consistency:remote\.consistency/.test(repo));
check('clearFeaturedHomeSurveyCache exists and is called after mutations',/function clearFeaturedHomeSurveyCache\(reason\)/.test(app)&&/clearFeaturedHomeSurveyCache\('survey_featured_admin_click'\)/.test(app)&&/clearFeaturedHomeSurveyCache\('survey_delete_or_archive'\)/.test(app)&&/clearFeaturedHomeSurveyCache\('remove_home_featured'\)/.test(app));
if(checks.some(c=>!c.ok))process.exit(1);
