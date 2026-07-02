const {read,must}=require('./legacy-public-final-validator-common');const f=read('functions/index.js');
must('publicSubmitPayloadFromRequest helper exists',/function publicSubmitPayloadFromRequest\(req\)/.test(f));
must('nested payload accepted',/raw\.payload[\s\S]*data\.payload/.test(f));
must('legacy survey alias accepted',/data\.surveyId\|\|data\.survey/.test(f));
must('legacy token aliases accepted',/data\.token\|\|data\.publicToken\|\|data\.accessToken/.test(f));
must('clear missing details',/code:'missing_survey_id'/.test(f)&&/code:'missing_public_token'/.test(f));
