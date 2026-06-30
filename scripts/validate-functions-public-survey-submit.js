const {assert,has,regex,done}=require('./_legacy-validator-lib');
['exports.submitSurveyResponse','exports.getPublicResult','exports.sendResultEmail','exports.getEmailStatus','exports.sendEmail','function isFreeOfficialSurvey','function isBetween','resultTokenHash','auditLog'].forEach(x=>assert(has('functions/index.js',x),`functions missing ${x}`));
assert(regex('functions/index.js',/isFreeOfficialSurvey\(s\).*options\.strict===true/s),'free official expiration bypass missing');done('functions public submit');
