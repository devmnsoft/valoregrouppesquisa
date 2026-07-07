const {ok,app}=require('./_legacy-final-validators');
ok(/isPublicRoute\(name,params\)[\s\S]*releasePublicUi/.test(app),'public route releases UI before auth gate');
ok(!/getPublicResult[\s\S]{0,120}(loadProfile|authedUser)/.test(app),'public result path does not call profile auth');
