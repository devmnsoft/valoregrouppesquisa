const {read,must}=require('./legacy-public-final-validator-common');const a=read('app.js');
must('unavailable renderer exists',/function renderPublicSurveyUnavailable/.test(a));
must('renderTakeSurvey blocks non-ready context',/stateNow\.status !== 'ready'[\s\S]*renderPublicSurveyUnavailable/.test(a));
must('form requires ready context before html',a.indexOf("stateNow.status !== 'ready'")<a.indexOf('data-public-survey-form'));
