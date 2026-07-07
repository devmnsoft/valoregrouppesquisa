const {ok,app,pdf}=require('./_legacy-final-validators');
ok(/function formatPublicDate/.test(app),'formatPublicDate helper exists');
ok(!/Invalid date/.test(app+pdf),'Invalid date literal absent');
