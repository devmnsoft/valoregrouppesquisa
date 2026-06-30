const fs=require('fs');const fail=[];const cfg=fs.readFileSync('config.js','utf8'),repo=fs.readFileSync('repository.js','utf8'),fb=fs.readFileSync('firebase-repository.js','utf8');
if(!/DATA_PROVIDER:\s*'firebase'/.test(cfg))fail.push('DATA_PROVIDER não é firebase');
if(!/PUBLIC_SUBMISSION_PROVIDER:\s*'cloud-functions'/.test(cfg))fail.push('PUBLIC_SUBMISSION_PROVIDER não é cloud-functions');
if(!/PUBLIC_SUBMISSION_FALLBACKS:\s*\['cloud-functions'\]/.test(cfg))fail.push('PUBLIC_SUBMISSION_FALLBACKS deve conter só cloud-functions');
if(!/RESULT_FALLBACKS:\s*\['cloud-functions'\]/.test(cfg))fail.push('RESULT_FALLBACKS deve conter só cloud-functions');
if(!/firebaseOnlyPublicOps/.test(repo))fail.push('repository não força operações públicas no Firebase');
if(!/callFunction\('submitSurveyResponse',\{payload\}\)/.test(fb))fail.push('firebase submit não chama submitSurveyResponse');
if(fail.length){console.error(fail.join('\n'));process.exit(1)}console.log('validate-public-submit-firebase-only: PASS');
