const fs=require('fs');
const f=fs.readFileSync('functions/index.js','utf8');
function ok(c,m){if(!c)throw new Error(m)}
ok(/exports\.preparePublicSurveyLink=onCall/.test(f),'preparePublicSurveyLink missing');
ok(/publicToken:tokenValue/.test(f),'publicToken not written');
ok(/tokenHash:sha256\(tokenValue\)/.test(f),'tokenHash not written');
ok(!/return \{[^}]*tokenHash/.test(f.match(/exports\.preparePublicSurveyLink=onCall[\s\S]*?\n\}\);/)?.[0]||''),'prepare returns tokenHash');
ok(/repairFeaturedHomeSurvey[\s\S]*preparePublicSurveyDocument/.test(f),'repair does not use helper');
ok(/autoRepairFeaturedHomeSurvey[\s\S]*preparePublicSurveyDocument/.test(f),'featured auto repair does not use helper');
ok(/featured_survey_invalid[\s\S]*rejectedCandidates/.test(f),'featured diagnostics missing');
ok(/public_survey_invalid[\s\S]*rejectedReasons/.test(f),'validate diagnostics missing');
ok(/responseId:ref\.id,resultToken,accessToken:resultToken/.test(f),'submit result contract missing');
console.log('validate-public-survey-contract: PASS');
